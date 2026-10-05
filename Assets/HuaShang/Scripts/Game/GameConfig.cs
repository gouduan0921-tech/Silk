using UnityEngine;
using HuaShang.Data;
using HuaShang.Rules.Config;

namespace HuaShang.Game
{
    /// <summary>运行时配置的唯一入口：从 ConfigDatabase 读出并校验（docs/05 §9），之后只读。</summary>
    public static class GameConfig
    {
        static ConfigSnapshot current;

        public static ConfigSnapshot Current
        {
            get
            {
                if (current == null) throw new System.InvalidOperationException("配置尚未加载，场景里需要 GameBootstrap 或先调用 GameConfig.Load。");
                return current;
            }
        }

        public static bool IsLoaded => current != null;

        public static ConfigSnapshot Load(ConfigDatabase db)
        {
            if (db == null) throw new System.ArgumentNullException(nameof(db));
            current = db.LoadValidated();
            return current;
        }

        /// <summary>测试或工具直接给快照。</summary>
        public static void Set(ConfigSnapshot snapshot) { current = snapshot; }

#if UNITY_EDITOR
        public const string DatabasePath = "Assets/HuaShang/Data/ConfigDatabase.asset";

        /// <summary>编辑器里按固定路径加载配置总表。</summary>
        public static ConfigSnapshot LoadInEditor()
        {
            var db = UnityEditor.AssetDatabase.LoadAssetAtPath<ConfigDatabase>(DatabasePath);
            if (db == null) throw new System.InvalidOperationException("找不到 " + DatabasePath + "，请先运行「HuaShang/从 docs 导入配置表」。");
            return Load(db);
        }
#endif
    }
}
