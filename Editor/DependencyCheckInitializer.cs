using UnityEditor;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class DependencyCheckInitializer
{
    static DependencyCheckInitializer()
    {
        EditorApplication.update += OpenInstallerOnStart;
    }

    private static void OpenInstallerOnStart()
    {
        EditorApplication.update -= OpenInstallerOnStart;
        if (!SessionState.GetBool("InstallerWindowOpened", false))
        {
            InstallerWindow.ShowWindow();
            SessionState.SetBool("InstallerWindowOpened", true); // only once per session
        }
    }
}