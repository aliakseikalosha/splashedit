using UnityEngine;
using System.IO;
using UnityEditor;
using UnityEditor.AssetImporters;
using Splashedit.RuntimeCode;

namespace SplashEdit.EditorCode
{
    [ScriptedImporter(1, "lua")]
    class LuaImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<LuaFile>();
            var luaCode = File.ReadAllText(ctx.assetPath);
            asset.Init(luaCode);
            asset.name = Path.GetFileName(ctx.assetPath);
            var text = new TextAsset(asset.LuaScript);

            ctx.AddObjectToAsset("Text", text);
            ctx.AddObjectToAsset("Script", asset);
            ctx.SetMainObject(text);
        }
    }
}