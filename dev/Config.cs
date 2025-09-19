using Newtonsoft.Json;

namespace FTP_Deploy_Client.dev
{
    public class Config
    {
        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string RemotePath { get; set; } = string.Empty;
        public string LocalPath { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public string ProcessArguments { get; set; } = string.Empty;
        public bool IsRestartProcessAfterUpload { get; set; } = true;
        public bool IsSFTP { get; set; } = true;
        public bool IsIncludeSubfoldersInUpload { get; set; } = false;
        public bool IsUsingNohup { get; set; } = false;
        public OverwriteMode OverWriteMode { get; set; }

        private static readonly string ConfigFilePath = "config.json";

        public static Config? Load(bool interactive = false)
        {
            Config config;

            if (File.Exists(ConfigFilePath))
            {
                try
                {
                    string json = File.ReadAllText(ConfigFilePath);
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

            config.Host = Prompt("SSH Host", config.Host);
            config.User = Prompt("SSH User", config.User);
            config.Password = ReadPassword("SSH Password: ", string.IsNullOrEmpty(config.Password) ? "" : "********");
            config.RemotePath = Prompt("Remote Path", config.RemotePath);
            config.LocalPath = Prompt("Local Path", config.LocalPath);
            config.IsIncludeSubfoldersInUpload = Prompt("Include Subfolders (y/n)", config.IsIncludeSubfoldersInUpload ? "y" : "n").ToLower() == "y";
            config.ProcessName = Prompt("Process Name", config.ProcessName);
            config.IsUsingNohup = Prompt("Use nohup (non terminal process) to start process (y/n)", config.IsUsingNohup ? "y" : "n").ToLower() == "y";
            config.ProcessArguments = Prompt("Process Arguments", config.ProcessArguments);
            config.IsRestartProcessAfterUpload = Prompt("Restart Process (y/n)", config.IsRestartProcessAfterUpload ? "y" : "n").ToLower() == "y";
            config.OverWriteMode = (OverwriteMode)Enum.Parse(typeof(OverwriteMode), Prompt("Overwrite Mode (0: OverwriteAll, 1: OverwriteNewer, 2: Skip)", ((int)config.OverWriteMode).ToString()), true);

            if (string.IsNullOrEmpty(config.Host) || string.IsNullOrEmpty(config.User) || string.IsNullOrEmpty(config.Password))
            {
                Console.WriteLine("Error: Missing required parameters.");
                return null;
            }

            try
            {
                File.WriteAllText(ConfigFilePath, JsonConvert.SerializeObject(config, Formatting.Indented));
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
                !string.IsNullOrWhiteSpace(Password)
                && !string.IsNullOrWhiteSpace(User)
                && !string.IsNullOrWhiteSpace(LocalPath)
                && !string.IsNullOrWhiteSpace(RemotePath)
                && !string.IsNullOrWhiteSpace(Host);

            bool processOk = !IsRestartProcessAfterUpload || !string.IsNullOrWhiteSpace(ProcessName);

            return hasBaseValues && processOk;
        }
    }
}

