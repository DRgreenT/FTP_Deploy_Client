using System;
using System.Diagnostics;

namespace FTP_Client
{
    class Program
    {
        static void ShowSummary(Config config)
        {
            Console.WriteLine("\n== Config Summary ==");
            Console.WriteLine($"Host:               {config.host}");
            Console.WriteLine($"User:               {config.user}");
            Console.WriteLine($"Remote Path:        {config.remotePath}");
            Console.WriteLine($"Local Path:         {config.localPath}");
            Console.WriteLine($"Include Subfolders: {config.includeSubfolder}");
            Console.WriteLine($"Process Name:       {config.processName}");
            Console.WriteLine($"Restart Process:    {config.restartProcess}");
            Console.WriteLine($"Overwrite Mode:     {config.overwriteMode}");
        }
        static void Main(string[] args)
        {
            bool forceInteractive = args.Contains("-j");
            bool autoConfirm = args.Contains("-y");
            bool isLoop = args.Contains("-l");

            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("== FTP Client Deployment Tool ==\n");
            
            if (forceInteractive && isLoop)
            {
                Console.WriteLine("The -j and -l flags cannot be used together.");
                Console.ReadLine();
                return;
            }

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

            if(!config.IsValid())
            {
                Console.WriteLine("No valid values in config file!");
                needsInput = true;
            }

            ShowSummary(config);

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
                    Stopwatch before = new Stopwatch();
                    before.Start();
                    Deployer.Deploy(config);
                    before.Stop();
                    Console.WriteLine($"Deployment was done in: {before.Elapsed.TotalSeconds.ToString("0.0")} seconds.");
                   
                    if (isLoop && autoConfirm && !needsInput)
                    {                      
                        Console.WriteLine("Press [Y] to deploy again, or [Esc] to exit.");
                        var key = Console.ReadKey(true);
                        if (key.Key != ConsoleKey.Y)
                        {
                            return;
                        }
                    }

                    Console.ReadLine();
                }
                while (isLoop && autoConfirm && !needsInput);

            }
            catch (Exception ex)
            {
                Console.WriteLine("Deployment failed: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}
