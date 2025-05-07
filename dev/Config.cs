using Newtonsoft.Json;

namespace FTP_Deploy_Client.dev
{
    public class Config
    {
        public string host { get; set; } = string.Empty;
        public string user { get; set; } = string.Empty;
        public string pass { get; set; } = string.Empty;
        public string remotePath { get; set; } = string.Empty;
        public string localPath { get; set; } = string.Empty;
        public string processName { get; set; } = string.Empty;
        public string processArguments { get; set; } = string.Empty;
        public bool restartProcess { get; set; } = true;
        public bool isSFTP { get; set; } = true;
        public bool includeSubfolder { get; set; } = false;
        public OverwriteMode overwriteMode { get; set; }

        private static readonly string configPath = "config.json";

        public static Config? Load(bool interactive = false)
        {
            Config config;

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<Config>(json)!;
                    Console.WriteLine("Configuration loaded.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error reading config.json: " + ex.Message);
                    return null;
                }
            }
            else
            {
                config = new Config();
                Console.WriteLine("No config.json found.");
            }

            if (!interactive)
                return config;

            Console.WriteLine("Entering interactive setup...");

            config.host = Prompt("SSH Host", config.host);
            config.user = Prompt("SSH User", config.user);
            config.pass = ReadPassword("SSH Password: ", string.IsNullOrEmpty(config.pass) ? "" : "********");
            config.remotePath = Prompt("Remote Path", config.remotePath);
            config.localPath = Prompt("Local Path", config.localPath);
            config.includeSubfolder = Prompt("Include Subfolders (y/n)", config.includeSubfolder ? "y" : "n").ToLower() == "y";
            config.processName = Prompt("Process Name", config.processName);
            config.processArguments = Prompt("Process Arguments", config.processArguments);
            config.restartProcess = Prompt("Restart Process (y/n)", config.restartProcess ? "y" : "n").ToLower() == "y";
            config.overwriteMode = (OverwriteMode)Enum.Parse(typeof(OverwriteMode), Prompt("Overwrite Mode (0: OverwriteAll, 1: OverwriteNewer, 2: Skip)", ((int)config.overwriteMode).ToString()), true);

            if (string.IsNullOrEmpty(config.host) || string.IsNullOrEmpty(config.user) || string.IsNullOrEmpty(config.pass))
            {
                Console.WriteLine("Error: Missing required parameters.");
                return null;
            }

            try
            {
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
                Console.WriteLine("Configuration saved to config.json.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save config.json: " + ex.Message);
            }
            Thread.Sleep(2000);
            return config;
        }

        private static string Prompt(string promptText, string current = "")
        {
            Console.Write($"{promptText}{(string.IsNullOrEmpty(current) ? "" : $" [{current}]")}: ");
            var input = Console.ReadLine();
            return string.IsNullOrWhiteSpace(input) ? current : input.Trim();
        }

        private static string ReadPassword(string prompt, string current = "")
        {
            Console.Write(prompt);
            string pass = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                {
                    pass = pass[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();            
            return string.IsNullOrEmpty(pass) ? current : pass;
        }


        public bool IsValid()
        {
            bool hasBaseValues =
                !string.IsNullOrWhiteSpace(pass)
                && !string.IsNullOrWhiteSpace(user)
                && !string.IsNullOrWhiteSpace(localPath)
                && !string.IsNullOrWhiteSpace(remotePath)
                && !string.IsNullOrWhiteSpace(host);

            bool processOk = !restartProcess || !string.IsNullOrWhiteSpace(processName);

            return hasBaseValues && processOk;
        }
    }
}

