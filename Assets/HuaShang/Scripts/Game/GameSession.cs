using System;
using System.IO;
using UnityEngine;
using HuaShang.Data;
using HuaShang.Play;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Game
{
    /// <summary>
    /// 一局游戏：配置、存档与写盘时机。界面通过 Run 调用玩法命令；命令成功后由解算模块重算 cloth，
    /// 离开工位时写整档（docs/21 §6）。
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public class GameSession : MonoBehaviour
    {
        public static GameSession I { get; private set; }

        public ConfigDatabase database;
        public string saveFileName = "huashang_save_1.json";

        /// <summary>测试用：非空时改用这个存档文件名，不碰玩家的存档。</summary>
        public static string SaveFileOverride;

        public ConfigSnapshot Config { get; private set; }
        public SaveRoot Save { get; private set; }
        public string SavePath { get; private set; }

        /// <summary>存档内容变化后触发；界面据此刷新侧架与顶栏。</summary>
        public event Action Changed;
        /// <summary>命令执行结果（成功或失败），界面据此弹出短提示。</summary>
        public event Action<Result> Ran;

        void Awake()
        {
            I = this;
            Config = GameConfig.IsLoaded ? GameConfig.Current : GameConfig.Load(database);
            SavePath = Path.Combine(Application.persistentDataPath, SaveFileOverride ?? saveFileName);
            LoadOrCreate();
        }

        void OnDestroy() { if (I == this) I = null; }

        public void LoadOrCreate()
        {
            try
            {
                Save = File.Exists(SavePath) ? SaveSerializer.ReadFile(SavePath) : NewGameFactory.Create(Config, Guid.NewGuid().ToString("N"));
            }
            catch (SaveSchemaException e)
            {
                Debug.LogError("[HuaShang] 存档无法读取，已另存旧档并开新档：" + e.Message);
                File.Copy(SavePath, SavePath + ".bad", true);
                Save = NewGameFactory.Create(Config, Guid.NewGuid().ToString("N"));
            }
            ClothSync.RecomputeAll(Save, Config); // S2；clothLocked 不覆盖（S3）
            Quests.Refresh(Save, Config);
            Changed?.Invoke();
        }

        public void StartNewGame()
        {
            Save = NewGameFactory.Create(Config, Guid.NewGuid().ToString("N"));
            ClothSync.RecomputeAll(Save, Config);
            WriteNow();
            Changed?.Invoke();
        }

        /// <summary>执行一个玩法命令。只有成功才通知界面并重算 cloth。</summary>
        public Result Run(Func<SaveRoot, ConfigSnapshot, Result> command)
        {
            var r = command(Save, Config);
            if (r.ok)
            {
                ClothSync.RecomputeAll(Save, Config);
                Changed?.Invoke();
            }
            Ran?.Invoke(r);
            return r;
        }

        public void NotifyChanged() => Changed?.Invoke();

        /// <summary>离开工位即写盘（docs/19 §3、docs/21 §6）。</summary>
        public void WriteNow()
        {
            try { SaveSerializer.WriteFile(SavePath, Save); }
            catch (Exception e) { Debug.LogError("[HuaShang] 写盘失败：" + e.Message); }
        }

        void OnApplicationQuit() => WriteNow();
    }
}
