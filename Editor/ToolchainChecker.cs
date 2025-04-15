using UnityEngine;
using System.Diagnostics;
using System.Linq;

public static class ToolchainChecker
{
    public static readonly string[] requiredTools = new[]
    {
        "mipsel-linux-gnu-addr2line",
        "mipsel-linux-gnu-ar",
        "mipsel-linux-gnu-as",
        "mipsel-linux-gnu-cpp",
        "mipsel-linux-gnu-elfedit",
        "mipsel-linux-gnu-g++",
        "mipsel-linux-gnu-gcc",
        "mipsel-linux-gnu-gcc-ar",
        "mipsel-linux-gnu-gcc-nm",
        "mipsel-linux-gnu-gcc-ranlib",
        "mipsel-linux-gnu-gcov",
        "mipsel-linux-gnu-ld",
        "mipsel-linux-gnu-nm",
        "mipsel-linux-gnu-objcopy",
        "mipsel-linux-gnu-objdump",
        "mipsel-linux-gnu-ranlib",
        "mipsel-linux-gnu-readelf",
        "mipsel-linux-gnu-size",
        "mipsel-linux-gnu-strings",
        "mipsel-linux-gnu-strip"
    };

    /// <summary>
    /// Checks for the availability of a given tool by using a system command.  
    /// "where" is used on Windows and "which" on other platforms.
    /// </summary>
    public static bool IsToolAvailable(string toolName)
    {
        string command = Application.platform == RuntimePlatform.WindowsEditor ? "where" : "which";

        try
        {
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = toolName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return !string.IsNullOrEmpty(output);
        }
        catch
        {
            return false;
        }
    }
}
