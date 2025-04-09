using UnityEngine;

namespace Splashedit.RuntimeCode
{

    [CreateAssetMenu(fileName = "NewLuaScript", menuName = "Lua Script", order = 1)]
    public class LuaFile : ScriptableObject
    {
        public TextAsset luaScript;
    }
}
