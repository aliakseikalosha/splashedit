using Splashedit.RuntimeCode;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LuaFile))]
public class LuaScriptAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        LuaFile luaScriptAsset = (LuaFile)target;
        
        // Allow user to drag-and-drop the Lua file
        luaScriptAsset.luaScript = (TextAsset)EditorGUILayout.ObjectField("Lua Script", luaScriptAsset.luaScript, typeof(TextAsset), false);
    }
}
