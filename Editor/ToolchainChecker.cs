using UnityEngine;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System;

public static class ToolchainChecker
{
    public static readonly string[] mipsToolSuffixes = new[]
    {
        "addr2line", "ar", "as", "cpp", "elfedit", "g++", "gcc", "gcc-ar", "gcc-nm",
        "gcc-ranlib", "gcov", "ld", "nm", "objcopy", "objdump", "ranlib", "readelf",
        "size", "strings", "strip"
    };

    /// <summary>
    /// Returns the full tool names to be checked, based on platform.
    /// </summary>
    public static string[] GetRequiredTools()
    {
        string prefix = Application.platform == RuntimePlatform.WindowsEditor
            ? "mipsel-none-elf-"
            : "mipsel-linux-gnu-";

        return mipsToolSuffixes.Select(s => prefix + s).ToArray();
    }

    /// <summary>
    /// Checks for availability of any tool (either full name like "make" or "mipsel-*").
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

            if (!string.IsNullOrEmpty(output))
                return true;

            // Additional fallback for MIPS tools on Windows in local MIPS path
            if (Application.platform == RuntimePlatform.WindowsEditor &&
                toolName.StartsWith("mipsel-none-elf-"))
            {
                string localMipsBin = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "mips", "mips", "bin");

                string fullPath = Path.Combine(localMipsBin, toolName + ".exe");
                return File.Exists(fullPath);
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
