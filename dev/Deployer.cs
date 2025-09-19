using Renci.SshNet;
using Renci.SshNet.Common;
using System.Runtime.Intrinsics.X86;

namespace FTP_Deploy_Client.dev
{
    public static class Deployer
    {
        public static void Deploy(Config config)
        {
            using var ssh = new SshClient(config.Host, config.User, config.Password);
            using var sftp = new SftpClient(config.Host, config.User, config.Password);

            ssh.Connect();
            Console.WriteLine("SSH connected.");

            if (IsProcessStopped(config, ssh))
            {
                sftp.Connect();
                UploadFiles(config, sftp);
                sftp.Disconnect();

                if (config.IsRestartProcessAfterUpload && !string.IsNullOrEmpty(config.ProcessName))
                    StartProcess(config, ssh);

                Console.WriteLine("Deployment finished.");
            }
            else
            {
                Console.WriteLine("Process not stopped. Deployment aborted.");
            }

            ssh.Disconnect();
        }

        private static bool IsProcessRunning(Config config, SshClient ssh, out SshCommand result)
        {
            result = null!;

            if (string.IsNullOrEmpty(config.ProcessName))
            {
                Console.WriteLine("No process name provided.");
                return false;
            }

            bool isRunning = false;
            int attempts = 0;
            const int maxAttempts = 5;


            Console.WriteLine($"Checking if process '{config.ProcessName}' is running...");

            while (!isRunning && attempts < maxAttempts)
            {
                attempts++;               

                result = ssh.RunCommand($"ps -eo user,pid,cmd | grep -v grep | grep {config.ProcessName}");

                if (string.IsNullOrWhiteSpace(result.Result))
                {
                    if (attempts < maxAttempts)
                    {
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    Console.WriteLine("Process is running.");
                    return true;
                }
            }
            Console.WriteLine("Process is not running.");
            return false;
        }

        private static bool IsProcessStopped(Config config, SshClient ssh)
        {
            return StopProcessIfRunning(config, ssh);
        }

        private static bool StopProcessIfRunning(Config config, SshClient ssh)
        {
            if (!IsProcessRunning(config, ssh, out var result))
                return true;

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

            Console.WriteLine($"Stopping process '{config.ProcessName}'...");
            ssh.RunCommand($"pkill -f {config.ProcessName}");

            int cursorPos = Console.CursorTop;
            string[] spinner = { "|", "/", "-", "\\" };
            int index = 0;
            bool stillRunning;

            string message = $"Waiting for process '{config.ProcessName}' to stop...";
            Console.Write(message);
            do
            {
                var check = ssh.RunCommand($"pgrep -f {config.ProcessName}");
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

            string[] allFiles = !config.IsIncludeSubfoldersInUpload
                ? Directory.GetFiles(config.LocalPath)
                : Directory.GetFiles(config.LocalPath, "*.*", SearchOption.AllDirectories);

            int total = allFiles.Length;
            int current = 0;
            List<string> copied = new List<string>();
            string errors = "";
            foreach (var file in allFiles)
            {
                current++;
                string remoteFileName = Path.GetFileName(file);
                string relativePath = Path.GetRelativePath(config.LocalPath, file).Replace('\\', '/');
                string remoteFullPath = $"{config.RemotePath}/{relativePath}";
                bool uploadFile = true;

                try
                {
                    var remoteAttrs = sftp.GetAttributes(remoteFullPath);
                    var localTime = File.GetLastWriteTime(file);
                    var remoteTime = remoteAttrs.LastWriteTime;

                    if (config.OverWriteMode == OverwriteMode.Skip)
                        uploadFile = false;
                    else if (config.OverWriteMode == OverwriteMode.OverwriteNewer && remoteTime >= localTime)
                        uploadFile = false;
                }
                catch (SftpPathNotFoundException)
                {
                    uploadFile = true;
                }

                if (uploadFile)
                {
                    try
                    {
                        string remoteDir = Path.GetDirectoryName(remoteFullPath)!.Replace('\\', '/');
                        EnsureRemoteDirectoryExists(sftp, remoteDir);
                        using var stream = File.OpenRead(file);
                        filesToUpload++;
                        sftp.UploadFile(stream, remoteFullPath, true);
                        copied.Add(file);
                    }
                    catch(Exception ex)
                    {
                        errors += "Error: " + file + " " + ex.Message + " " + remoteFullPath + " \n";
                    }
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
            foreach (var file in copied)
                Console.Write(file.ToString() + " copied\n");

            Console.WriteLine(errors);
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
            Console.WriteLine($"Restarting process '{config.ProcessName}'...");
            string arguments = EscapeLitarals(config.ProcessArguments);
            string nohupPrefix = config.IsUsingNohup ? "nohup " : "";
            string logFile = $"{config.RemotePath}/last_start.log";

            string fullcommand = $"{nohupPrefix}{config.RemotePath}/{config.ProcessName} {arguments} > {logFile} 2>&1 &";

            ssh.RunCommand(fullcommand);

            Thread.Sleep(2000); 
            
            if (!IsProcessRunning(config, ssh, out var result))
            {
                Console.WriteLine("=== Command Feedback ===");
                Console.WriteLine($"Command   : {fullcommand}");

                var logResult = ssh.RunCommand($"tail -n 20 {logFile}");
                Console.WriteLine("Last log output:");
                Console.WriteLine(logResult.Result);
            }
        }


        public static string EscapeLitarals(string arguments)
        {
            if(string.IsNullOrEmpty(arguments)) return string.Empty;

            var arg = arguments.Split(' ');
            string newArguments = string.Empty;

            for (int i = 0; i < arg.Length; i++)
            {
                if (arg[i].StartsWith('$') || arg[i].StartsWith('*') || arg[i].StartsWith('"') || arg[i].StartsWith('&'))
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
