# FTP_Deploy_Client v1.0

A lightweight and flexible CLI tool for **secure SSH/SFTP-based deployments from Windows to Linux servers**, built with **.NET 9.0**.

<img src="https://github.com/DRgreenT/FTP_Deploy_Client/blob/master/docs/pic1.png" alt="Pic_1" width="800"/>>

- Upload application files  
- Stop and restart remote processes via SSH  
- Use `config.json` or interactive CLI input  
- Smart overwrite modes (`OverwriteAll`, `OverwriteNewer`, `Skip`)  
- Auto-creates `config.json` with default values  
- Auto-saves valid configurations
- Planned: auto upload if content of local folder changes

---

## Usage: 

### First Start

- First start creates a default config.json file
- You will be prompted for the required values

---

### Config.json

```
{
  "host": "192.168.178.10",                      // Host address (IP or domain)
  "user": "thomas",                              // SSH username
  "pass": "******",                              // SSH password (plain text)
  "remotePath": "/home/thomas",                  // Target path on the remote machine
  "localPath": "C:\\Users\\thoma\\source\\",     // Local path to source files
  "processName": "Test,                          // Process name to stop/restart on the server
  "processArguments": "--nmap",                  // Arguments for the process" 
  "restartProcess": true,                        // Restart the process after deployment
  "isSFTP": true,                                // Must be true (uses SFTP)
  "includeSubfolder": true,                      // Include subfolders in the upload
  "overwriteMode": 0                             // Overwrite behavior:
                                                 // 0 = Overwrite all
                                                 // 1 = Overwrite only if newer
                                                 // 2 = Skip existing files
}

```

---

### Command Line Parameters

```bash
-j      // Start with parameter -j to edit the config.json file 
-y      // Start with parameter -y to skip the confirmation
-l      // Start with parameter -l loop mode
-nS     // Program window close at the end 
```

If you want to use ```-l``` please consider:
- it will set skip confirmation to true too
- ```-j``` and ```-l``` will not work together

---

## Download & Setup

### Requirements

- **Windows 10/11** with [.NET 9.0 Runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
- A **Linux server** (e.g., Ubuntu, Debian) with:
  - SSH server running (`sshd`)
  - User account with SFTP access and permission to stop/start the target process

---

## Clone and build (Windows):
   ```bash
   git clone https://github.com/your-username/ftp_deploy_client.git
   cd ftp_deploy_client
   build.bat
   ```
   or use Visual Studio 2022+ to open the solution file and build the project.

---

## Disclaimer

The developer assumes no responsibility for:

    Any damage, data loss, or service disruptions caused by the use or misuse of this software
    Any malfunction due to system incompatibilities or environmental factors

By using this software, you agree to use it at your own risk.
The developer is not liable for any damages or losses incurred as a result of using this software.