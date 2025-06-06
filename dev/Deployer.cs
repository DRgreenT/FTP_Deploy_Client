﻿using Renci.SshNet;
using Renci.SshNet.Common;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace FTP_Deploy_Client.dev
{
    public static class Deployer
    {
        public static void Deploy(Config config)
        {
            using var ssh = new SshClient(config.host, config.user, config.pass);
            using var sftp = new SftpClient(config.host, config.user, config.pass);

            ssh.Connect();
            Console.WriteLine("SSH connected.");

            if (StopProcessIfRunning(config, ssh))
            {
                sftp.Connect();
                UploadFiles(config, sftp);
                sftp.Disconnect();

                if (config.restartProcess && !string.IsNullOrEmpty(config.processName))
                    StartProcess(config, ssh);

                Console.WriteLine("Deployment finished.");
            }
            else
            {
                Console.WriteLine("Process not stopped. Deployment aborted.");
            }

            ssh.Disconnect();
        }

        private static bool StopProcessIfRunning(Config config, SshClient ssh)
        {
            if (string.IsNullOrEmpty(config.processName))
                return false;

            Console.WriteLine($"Checking if process '{config.processName}' is running...");
            var result = ssh.RunCommand($"ps -eo user,pid,cmd | grep -v grep | grep {config.processName}");

            if (string.IsNullOrWhiteSpace(result.Result))
            {
                Console.WriteLine("Process is not running.");
                return true;
            }

            string[] lines = result.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string currentUser = ssh.RunCommand("whoami").Result.Trim();
            bool canStop = false;

            foreach (string line in lines)
            {
                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                string owner = parts[0];
                string pid = parts[1];
                string cmd = string.Join(' ', parts, 2, parts.Length - 2);

                Console.WriteLine($"Found process: PID={pid}, User={owner}, Command={cmd}");

                if (owner == currentUser)
                    canStop = true;
                else
                    Console.WriteLine($"Warning: Process owned by '{owner}'. You are '{currentUser}'. Insufficient permission.");
            }

            if (!canStop)
            {
                Console.WriteLine("Cannot stop process. You do not own it. Consider using sudo.");
                return false;
            }

            Console.WriteLine($"Stopping process '{config.processName}'...");
            ssh.RunCommand($"pkill -f {config.processName}");

            int cursorPos = Console.CursorTop;
            string[] spinner = { "|", "/", "-", "\\" };
            int index = 0;
            bool stillRunning;

            string message = $"Waiting for process '{config.processName}' to stop...";
            Console.Write(message);
            do
            {
                var check = ssh.RunCommand($"pgrep -f {config.processName}");
                stillRunning = !string.IsNullOrWhiteSpace(check.Result);

                if (stillRunning)
                {
                    Console.SetCursorPosition(message.Length + 1, cursorPos);
                    Console.Write(spinner[index]);
                    Thread.Sleep(150);
                    index = index < spinner.Length - 1 ? index + 1 : 0;
                }
            }
            while (stillRunning);
            Thread.Sleep(2000);
            Console.WriteLine("\nProcess has stopped.");
            return true;
        }

        private static void UploadFiles(Config config, SftpClient sftp)
        {
            Console.WriteLine("SFTP connected. Starting file upload...\n");
            int lineNr = Console.CursorTop;
            int filesToUpload = 0;
            int skipped = 0;

            string[] allFiles = !config.includeSubfolder
                ? Directory.GetFiles(config.localPath)
                : Directory.GetFiles(config.localPath, "*.*", SearchOption.AllDirectories);

            int total = allFiles.Length;
            int current = 0;

            foreach (var file in allFiles)
            {
                current++;
                string remoteFileName = Path.GetFileName(file);
                string relativePath = Path.GetRelativePath(config.localPath, file).Replace('\\', '/');
                string remoteFullPath = $"{config.remotePath}/{relativePath}";
                bool uploadFile = true;

                try
                {
                    var remoteAttrs = sftp.GetAttributes(remoteFullPath);
                    var localTime = File.GetLastWriteTime(file);
                    var remoteTime = remoteAttrs.LastWriteTime;

                    if (config.overwriteMode == OverwriteMode.Skip)
                        uploadFile = false;
                    else if (config.overwriteMode == OverwriteMode.OverwriteNewer && remoteTime >= localTime)
                        uploadFile = false;
                }
                catch (SftpPathNotFoundException)
                {
                    uploadFile = true;
                }

                if (uploadFile)
                {
                    string remoteDir = Path.GetDirectoryName(remoteFullPath)!.Replace('\\', '/');
                    EnsureRemoteDirectoryExists(sftp, remoteDir);

                    using var stream = File.OpenRead(file);
                    filesToUpload++;
                    sftp.UploadFile(stream, remoteFullPath, true);
                }
                else
                {
                    skipped++;
                }
                Console.SetCursorPosition(0, lineNr);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, lineNr);
                Console.Write($"File {current} of {total}: {remoteFileName}");
            }

            Console.WriteLine($"\nFile upload completed. Uploaded: {filesToUpload}, Skipped: {skipped}\n");
        }

        private static void EnsureRemoteDirectoryExists(SftpClient sftp, string remoteDir)
        {
            string[] parts = remoteDir.Split('/', StringSplitOptions.RemoveEmptyEntries);
            string path = "";
            foreach (string part in parts)
            {
                path += "/" + part;
                if (!sftp.Exists(path))
                {
                    sftp.CreateDirectory(path);
                }
            }
        }

        private static void StartProcess(Config config, SshClient ssh)
        {
            Console.WriteLine($"Restarting process '{config.processName}'...");
            string arguments = EscapeLitarals(config.processArguments);
            ssh.RunCommand($"{config.remotePath}/{config.processName} " + arguments);
        }

        public static string EscapeLitarals(string arguments)
        {
            if(string.IsNullOrEmpty(arguments)) return string.Empty;

            var arg = arguments.Split(' ');
            string newArguments = string.Empty;

            for (int i = 0; i < arg.Length; i++)
            {
                if (arg[i].StartsWith('$') || arg[i].StartsWith('*') || arg[i].StartsWith('"') || arg[i].StartsWith("&"))
                {
                    string newArg = "'" + arg[i] + "'";
                    arg[i] = newArg;
                }
                arg[i] = i == arg.Length - 1 ? arg[i] : arg[i] + " ";
                newArguments += arg[i];              
            }
            return newArguments;
        }
    }
}
