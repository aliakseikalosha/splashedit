using Splashedit.RuntimeCode;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LuaFile))]
public class LuaScriptAssetEditor : Editor
{
    private TextAsset asset;

    public override void OnInspectorGUI()
    {
        LuaFile luaScriptAsset = (LuaFile)target;
        EditorGUILayout.TextArea(luaScriptAsset.LuaScript);
    }
}
