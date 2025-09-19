using FTP_Deploy_Client.dev;
using System;
using System.Diagnostics;

namespace FTP_Client
{
    class Program
    {
        static void ShowSummary(Config config)
        {
            Console.WriteLine("\n== Config summary ==");

            int labelWidth = 31; 
            void Print(string label, object value) =>
                Console.WriteLine($"{label.PadRight(labelWidth)} {value}");

            Print("Hostname:", config.Host);
            Print("Username:", config.User);
            Print("Program path (remote):", config.RemotePath);
            Print("Local path (source):", config.LocalPath);
            Print("Include subfolders in upload:", config.IsIncludeSubfoldersInUpload);
            Print("Process name:", config.ProcessName);
            Print("Restart process after update:", config.IsRestartProcessAfterUpload);
            Print("Use nohup:", config.IsUsingNohup);
            Print("Process arguments:", config.ProcessArguments);
            Print("Overwrite mode:", config.OverWriteMode);
        }

        static void Main(string[] args)
        {
            bool forceInteractive = args.Contains("-j");
            bool autoConfirm = args.Contains("-y");
            bool isNoStop = args.Contains("-nS");
            bool isLoop = args.Contains("-l");

            if (forceInteractive && isLoop)
            {
                Console.WriteLine("The -j and -l flags cannot be used together.");
                Console.ReadLine();
                return;
            }
            if (isNoStop && isLoop)
            {
                Console.WriteLine("The -nS and -l flags cannot be used together.");
                Console.ReadLine();
                return;
            }

            Console.Clear();
            Console.SetCursorPosition(0, 0);

            Console.WriteLine("== FTP Client Deployment Tool ==\n");

            if (isLoop && !autoConfirm && !forceInteractive)
            {
                Console.WriteLine("In order to use the loop function, autoconfirm is now set to true!");
                autoConfirm = true;
            }

            bool configExists = File.Exists("config.json");
            bool needsInput = forceInteractive || !configExists;

            Config? config = Config.Load(needsInput);
            
            if (config == null)
            {
                Console.WriteLine("Deployment aborted due to invalid config. Use -j flag to reset the config file.");
                Console.ReadLine();
                return;
            }

            if (!config.IsValid())
            {
                int cursorPos = Console.CursorTop;
                Console.WriteLine("Config file does not contain valid values!");
                config = Config.Load(true);
                for (int i = cursorPos; i < 30; i++)
                {
                    Console.SetCursorPosition(0, i);
                    Console.Write(new string(' ', Console.WindowWidth));
                }
                Console.SetCursorPosition(0, cursorPos);
            }

            ShowSummary(config!);

            if (!autoConfirm && !needsInput)
            {
                Console.WriteLine("\nPress [Y] to deploy, or [Esc] to abort.");

                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Y)
                {
                    Console.WriteLine("Aborted.");
                    Console.ReadLine();
                    return;
                }
            }
            
            try
            {

                Console.WriteLine("\n==Deployment==");
                int cursorPos = Console.CursorTop + 1;

                do
                {

                    Console.SetCursorPosition(0, cursorPos);
                    Console.CursorVisible = false;

                    Stopwatch before = new Stopwatch();
                    before.Start();
                    Deployer.Deploy(config!);
                    before.Stop();
                    Console.WriteLine($"Deployment was done in: {before.Elapsed.TotalSeconds.ToString("0.0")} seconds.\n");
                    Console.CursorVisible = true;

                    if (isLoop && autoConfirm && !needsInput)
                    {                      
                        Console.WriteLine("Press [Y] to deploy again, or [Esc] to exit.");
                        var key = Console.ReadKey(true);
                        if (key.Key != ConsoleKey.Y)
                        {
                            return;
                        }
                    }

                    if(!isNoStop)
                       Console.ReadLine();
                }
                while (isLoop && autoConfirm && !needsInput);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Deployment failed: " + ex.Message + "\n");
                if(!isNoStop)                
                    Console.ReadLine();
            }
        }
    }
}
