using System;
using UnityEngine;
using HuaShang.Solve;

namespace HuaShang.Game
{
    /// <summary>界面设置，不属于 schema 1 存档（构架文档 §4.2），存在 PlayerPrefs。</summary>
    [Serializable]
    public class Settings
    {
        public bool largeText;
        public bool reduceMotion;
        public bool subtitles = true; // 字幕默认开（docs/01 §8）
        public float musicVolume = 0.7f;
        public float operationVolume = 1f;
        public int clothQuality = (int)ClothQuality.High; // 默认布料质量高（docs/22 §3）

        const string Key = "HuaShang.Settings";
        static Settings current;

        public static event Action Changed;

        public static Settings Current
        {
            get
            {
                if (current == null)
                {
                    try { current = JsonUtility.FromJson<Settings>(PlayerPrefs.GetString(Key, "")) ?? new Settings(); }
                    catch { current = new Settings(); }
                    if (current == null) current = new Settings();
                }
                return current;
            }
        }

        /// <summary>放大文字到标准的 1.13 倍（风格规范.md「文字」），不动命中区。</summary>
        public float TextScale => largeText ? 1.13f : 1f;

        public static void Apply()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
