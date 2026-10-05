using UnityEngine;
using HuaShang.Data;

namespace HuaShang.Game
{
    /// <summary>场景入口：加载并校验配置（docs/05 §9），失败则不进入游戏。</summary>
    [DefaultExecutionOrder(-1000)]
    public class GameBootstrap : MonoBehaviour
    {
        public ConfigDatabase database;

        void Awake()
        {
            if (!GameConfig.IsLoaded) GameConfig.Load(database);
        }
    }
}
