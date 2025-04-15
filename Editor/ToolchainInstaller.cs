using System;
using System.Diagnostics;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.IO;

public static class ToolchainInstaller
{
    // Flags to prevent duplicate installations.
    private static bool mipsInstalling = false;
    private static bool win32MipsToolsInstalling = false;

    // The version string used by the installer command.
    public static string mipsVersion = "14.2.0";

    /// <summary>
    /// Executes an external process asynchronously.
    /// Throws an exception if the process returns a nonzero exit code.
    /// </summary>
    public static async Task RunCommandAsync(string fileName, string arguments, string workingDirectory = "")
    {
        var tcs = new TaskCompletionSource<int>();

        if (fileName.Equals("mips", StringComparison.OrdinalIgnoreCase))
        {
            fileName = "powershell";

            // Get the AppData\Roaming path for the user
            string roamingPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
            string scriptPath = Path.Combine(roamingPath, "mips\\mips.ps1");

            // Pass the arguments to the PowerShell script
            arguments = $"-ExecutionPolicy Bypass -File \"{scriptPath}\" {arguments}"; 
        }


        Process process = new Process();
        process.StartInfo.FileName = fileName;
        process.StartInfo.Arguments = arguments;
        process.StartInfo.CreateNoWindow = false;
        process.StartInfo.UseShellExecute = true;

        if (!string.IsNullOrEmpty(workingDirectory))
            process.StartInfo.WorkingDirectory = workingDirectory;

        process.EnableRaisingEvents = true;
        process.Exited += (sender, args) =>
        {
            tcs.SetResult(process.ExitCode);
            process.Dispose();
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to start process {fileName}: {ex.Message}");
        }

        int exitCode = await tcs.Task;
        if (exitCode != 0)
            throw new Exception($"Process '{fileName} {arguments}' exited with code {exitCode}");
    }


    #region MIPS Toolchain Installation

    /// <summary>
    /// Installs the MIPS toolchain on Windows using a PowerShell script.
    /// (On Windows this installer bundles GNU Make as part of the toolchain.)
    /// </summary>
    public static async Task InstallMips()
    {
        if (mipsInstalling) return;
        mipsInstalling = true;
        try
        {
            // Download and run the installer script via PowerShell.
            await RunCommandAsync("powershell", 
                "-c \"& { iwr -UseBasicParsing https://raw.githubusercontent.com/grumpycoders/pcsx-redux/main/mips.ps1 | iex }\"");
            EditorUtility.DisplayDialog("Reboot Required", 
                "Installing the MIPS toolchain requires a reboot. Please reboot your computer and click the button again.", 
                "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Error", 
                "An error occurred while installing the MIPS toolchain. Please install it manually.", "OK");
            throw ex;
        }
    }

    /// <summary>
    /// Installs the MIPS toolchain based on the current platform.
    /// Uses pkexec on Linux to request graphical elevation.
    /// </summary>
    public static async Task<bool> InstallToolchain()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
                try
                {
                    if (!ToolchainChecker.IsToolAvailable("mips"))
                    {
                        await InstallMips();
                        return false;
                    }
                    else
                    {
                        if (win32MipsToolsInstalling) return false;
                        win32MipsToolsInstalling = true;
                        await RunCommandAsync("mips", $"install {mipsVersion}");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing the MIPS toolchain. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            case RuntimePlatform.LinuxEditor:
                try
                {
                    if (ToolchainChecker.IsToolAvailable("apt"))
                    {
                        await RunCommandAsync("pkexec", "apt install g++-mipsel-linux-gnu -y");
                    }
                    else if (ToolchainChecker.IsToolAvailable("trizen"))
                    {
                        await RunCommandAsync("trizen", "-S cross-mipsel-linux-gnu-binutils cross-mipsel-linux-gnu-gcc");
                    }
                    else if (ToolchainChecker.IsToolAvailable("brew"))
                    {
                        string binutilsScriptPath = Application.dataPath + "/Scripts/mipsel-none-elf-binutils.rb";
                        string gccScriptPath = Application.dataPath + "/Scripts/mipsel-none-elf-gcc.rb";
                        await RunCommandAsync("brew", $"install --formula \"{binutilsScriptPath}\" \"{gccScriptPath}\"");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", 
                            "Your Linux distribution is not supported. Please install the MIPS toolchain manually.", "OK");
                        throw new Exception("Unsupported Linux distribution");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing the MIPS toolchain. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            case RuntimePlatform.OSXEditor:
                try
                {
                    if (ToolchainChecker.IsToolAvailable("brew"))
                    {
                        string binutilsScriptPath = Application.dataPath + "/Scripts/mipsel-none-elf-binutils.rb";
                        string gccScriptPath = Application.dataPath + "/Scripts/mipsel-none-elf-gcc.rb";
                        await RunCommandAsync("brew", $"install --formula \"{binutilsScriptPath}\" \"{gccScriptPath}\"");
                    }
                    else
                    {
                        await RunCommandAsync("/bin/bash", "-c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"");
                        EditorUtility.DisplayDialog("Reboot Required", 
                            "Installing Homebrew requires a reboot. Please reboot your computer before proceeding further.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing the MIPS toolchain. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            default:
                EditorUtility.DisplayDialog("Error", 
                    "Your platform is not supported by this extension. Please install the MIPS toolchain manually.", "OK");
                throw new Exception("Unsupported platform");
        }
        return true;
    }

    #endregion

    #region GNU Make Installation

    /// <summary>
    /// Installs GNU Make.  
    /// On Linux/macOS it installs GNU Make normally.  
    /// On Windows, GNU Make is bundled with the MIPS toolchain—so the user is warned before proceeding.
    /// </summary>
    public static async Task InstallMake()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
                // Inform the user that GNU Make is bundled with the MIPS toolchain.
                bool proceed = EditorUtility.DisplayDialog(
                    "Install GNU Make",
                    "On Windows, GNU Make is installed as part of the MIPS toolchain installer. Would you like to install the full toolchain?",
                    "Yes",
                    "No"
                );
                if (proceed)
                {
                    await InstallToolchain();
                }
                break;

            case RuntimePlatform.LinuxEditor:
                try
                {
                    if (ToolchainChecker.IsToolAvailable("apt"))
                    {
                        await RunCommandAsync("pkexec", "apt install build-essential -y");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", 
                            "Your Linux distribution is not supported. Please install GNU Make manually.", "OK");
                        throw new Exception("Unsupported Linux distribution");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing GNU Make. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            case RuntimePlatform.OSXEditor:
                try
                {
                    if (ToolchainChecker.IsToolAvailable("brew"))
                    {
                        await RunCommandAsync("brew", "install make");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", 
                            "Homebrew is not installed. Please install GNU Make manually.", "OK");
                        throw new Exception("Brew not installed");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing GNU Make. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            default:
                EditorUtility.DisplayDialog("Error", 
                    "Your platform is not supported. Please install GNU Make manually.", "OK");
                throw new Exception("Unsupported platform");
        }
    }

    #endregion

    #region GDB Installation (Optional)

    /// <summary>
    /// Installs GDB Multiarch (or GDB on macOS)
    /// </summary>
    public static async Task InstallGDB()
    {
        switch (Application.platform)
        {
            case RuntimePlatform.WindowsEditor:
                try
                {
                    if (!ToolchainChecker.IsToolAvailable("mips"))
                    {
                        await InstallMips();
                    }
                    else
                    {
                        if (win32MipsToolsInstalling) return;
                        win32MipsToolsInstalling = true;
                        await RunCommandAsync("mips", $"install {mipsVersion}");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing GDB Multiarch. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            case RuntimePlatform.LinuxEditor:
                try
                {
                    if (ToolchainChecker.IsToolAvailable("apt"))
                    {
                        await RunCommandAsync("pkexec", "apt install gdb-multiarch -y");
                    }
                    else if (ToolchainChecker.IsToolAvailable("trizen"))
                    {
                        await RunCommandAsync("trizen", "-S gdb-multiarch");
                    }
                    else if (ToolchainChecker.IsToolAvailable("brew"))
                    {
                        await RunCommandAsync("brew", "install gdb-multiarch");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", 
                            "Your Linux distribution is not supported. Please install GDB Multiarch manually.", "OK");
                        throw new Exception("Unsupported Linux distribution");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing GDB Multiarch. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            case RuntimePlatform.OSXEditor:
                try
                {
                    if (ToolchainChecker.IsToolAvailable("brew"))
                    {
                        await RunCommandAsync("brew", "install gdb");
                    }
                    else
                    {
                        await RunCommandAsync("/bin/bash", "-c \"$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\"");
                        EditorUtility.DisplayDialog("Reboot Required", 
                            "Installing Homebrew requires a reboot. Please reboot your computer before proceeding further.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Error", 
                        "An error occurred while installing GDB Multiarch. Please install it manually.", "OK");
                    throw ex;
                }
                break;

            default:
                EditorUtility.DisplayDialog("Error", 
                    "Your platform is not supported. Please install GDB Multiarch manually.", "OK");
                throw new Exception("Unsupported platform");
        }
    }

    #endregion

    
}
