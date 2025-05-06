using System;

namespace FTP_Client
{
    class Program
    {
        static void Main(string[] args)
        {
            bool forceInteractive = args.Contains("-j");
            bool autoConfirm = args.Contains("-y");

            bool configExists = File.Exists("config.json");
            bool needsInput = forceInteractive || !configExists;

            Config? config = Config.Load(needsInput);

            if (config == null)
            {
                Console.WriteLine("Deployment aborted due to invalid config.");
                return;
            }

            if (!autoConfirm && !needsInput)
            {
                Console.WriteLine("\n== Config Summary ==");
                Console.WriteLine($"Host:           {config.host}");
                Console.WriteLine($"User:           {config.user}");
                Console.WriteLine($"Remote Path:    {config.remotePath}");
                Console.WriteLine($"Local Path:     {config.localPath}");
                Console.WriteLine($"Process Name:   {config.processName}");
                Console.WriteLine($"Restart Process:{config.restartProcess}");
                Console.WriteLine($"Overwrite Mode: {config.overwriteMode}");

                Console.WriteLine("\nPress [Y] to deploy, or [Esc] to abort.");

                var key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Y)
                {
                    Console.WriteLine("Aborted.");
                    return;
                }
            }

            try
            {
                Deployer.Deploy(config);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Deployment failed: " + ex.Message);
            }
        }
    }
}
