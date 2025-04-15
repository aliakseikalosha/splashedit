using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using SplashEdit.RuntimeCode;

public class InstallerWindow : EditorWindow
{
    // Cached status for MIPS toolchain binaries.
    private Dictionary<string, bool> mipsToolStatus = new Dictionary<string, bool>();

    // Cached status for optional tools.
    private bool makeInstalled;
    private bool gdbInstalled;
    private string pcsxReduxPath;

    private bool isInstalling = false;

    [MenuItem("PSX/Toolchain & Build Tools Installer")]
    public static void ShowWindow()
    {
        InstallerWindow window = GetWindow<InstallerWindow>("Toolchain Installer");
        window.RefreshToolStatus();
        window.pcsxReduxPath = DataStorage.LoadData().PCSXReduxPath;
    }

    /// <summary>
    /// Refresh the cached statuses for all tools.
    /// </summary>
    private void RefreshToolStatus()
    {
        mipsToolStatus.Clear();
        foreach (var tool in ToolchainChecker.requiredTools)
        {
            mipsToolStatus[tool] = ToolchainChecker.IsToolAvailable(tool);
        }

        makeInstalled = ToolchainChecker.IsToolAvailable("make");
        gdbInstalled = ToolchainChecker.IsToolAvailable("gdb-multiarch");
    }

    private void OnGUI()
    {
        GUILayout.Label("Toolchain & Build Tools Installer", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Refresh Status"))
        {
            RefreshToolStatus();
        }
        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        DrawToolchainColumn();
        DrawAdditionalToolsColumn();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolchainColumn()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width / 2 - 10));
        GUILayout.Label("MIPS Toolchain", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // Display cached status for each required MIPS tool.
        foreach (var kvp in mipsToolStatus)
        {
            GUI.color = kvp.Value ? Color.green : Color.red;
            GUILayout.Label($"{kvp.Key}: {(kvp.Value ? "Found" : "Missing")}");
        }
        GUI.color = Color.white;
        GUILayout.Space(5);

        if (GUILayout.Button("Install MIPS Toolchain"))
        {
            if (!isInstalling)
                InstallMipsToolchainAsync();
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawAdditionalToolsColumn()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.MaxWidth(position.width / 2 - 10));
        GUILayout.Label("Optional Tools", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // GNU Make status (required).
        GUI.color = makeInstalled ? Color.green : Color.red;
        GUILayout.Label($"GNU Make: {(makeInstalled ? "Found" : "Missing")} (Required)");
        GUI.color = Color.white;
        GUILayout.Space(5);
        if (GUILayout.Button("Install GNU Make"))
        {
            if (!isInstalling)
                InstallMakeAsync();
        }

        GUILayout.Space(10);

        // GDB status (optional).
        GUI.color = gdbInstalled ? Color.green : Color.red;
        GUILayout.Label($"GDB: {(gdbInstalled ? "Found" : "Missing")} (Optional)");
        GUI.color = Color.white;
        GUILayout.Space(5);
        if (GUILayout.Button("Install GDB"))
        {
            if (!isInstalling)
                InstallGDBAsync();
        }

        GUILayout.Space(10);

        // PCSX-Redux (manual install)
        GUI.color = string.IsNullOrEmpty(pcsxReduxPath) ? Color.red : Color.green;
        GUILayout.Label($"PCSX-Redux: {(string.IsNullOrEmpty(pcsxReduxPath) ? "Not Configured" : "Configured")} (Optional)");
        GUI.color = Color.white;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Browse for PCSX-Redux"))
        {
            string selectedPath = EditorUtility.OpenFilePanel("Select PCSX-Redux Executable", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                pcsxReduxPath = selectedPath;
                PSXData data = DataStorage.LoadData();
                data.PCSXReduxPath = pcsxReduxPath;
                DataStorage.StoreData(data);
            }
        }
        if (!string.IsNullOrEmpty(pcsxReduxPath))
        {
            if (GUILayout.Button("Clear", GUILayout.Width(60)))
            {
                pcsxReduxPath = "";
                PSXData data = DataStorage.LoadData();
                data.PCSXReduxPath = pcsxReduxPath;
                DataStorage.StoreData(data);
            }
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    private async void InstallMipsToolchainAsync()
    {
        try
        {
            isInstalling = true;
            EditorUtility.DisplayProgressBar("Installing MIPS Toolchain",
                "Please wait while the MIPS toolchain is being installed...", 0f);
            await ToolchainInstaller.InstallToolchain();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Installation Complete", "MIPS toolchain installed successfully.", "OK");
            RefreshToolStatus(); // Update cached statuses after installation
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Installation Failed", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            isInstalling = false;
        }
    }

    private async void InstallMakeAsync()
    {
        try
        {
            isInstalling = true;
            EditorUtility.DisplayProgressBar("Installing GNU Make",
                "Please wait while GNU Make is being installed...", 0f);
            await ToolchainInstaller.InstallMake();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Installation Complete", "GNU Make installed successfully.", "OK");
            RefreshToolStatus();
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Installation Failed", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            isInstalling = false;
        }
    }

    private async void InstallGDBAsync()
    {
        try
        {
            isInstalling = true;
            EditorUtility.DisplayProgressBar("Installing GDB",
                "Please wait while GDB is being installed...", 0f);
            await ToolchainInstaller.InstallGDB();
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Installation Complete", "GDB installed successfully.", "OK");
            RefreshToolStatus();
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Installation Failed", $"Error: {ex.Message}", "OK");
        }
        finally
        {
            isInstalling = false;
        }
    }
}
