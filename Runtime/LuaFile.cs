using UnityEngine;

namespace Splashedit.RuntimeCode
{
    public class LuaFile : ScriptableObject
    {
        [SerializeField] private string luaScript;
        public string LuaScript => luaScript;

        public void Init(string luaCode)
        {
            luaScript = luaCode;
        }
    }
}
