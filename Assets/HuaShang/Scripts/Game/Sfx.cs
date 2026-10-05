using UnityEngine;

namespace HuaShang.Game
{
    /// <summary>
    /// 操作声（docs/18）：程序合成的占位音，正式音效交付前使用。操作声与音乐分开控制，关掉音乐操作声仍在（发布门第 9 条）。
    /// 稳的一梭清脆，乱的一梭带摩擦，断头一声短响，搅拌乱时水声变碎，针距偏时布声变干；素纱摩擦轻短、绸更沉。
    /// </summary>
    public class Sfx : MonoBehaviour
    {
        public static Sfx I { get; private set; }

        public enum Cue { ShuttleSteady, ShuttleRough, Break, Needle, NeedleDry, Water, WaterBroken, Wood, ClothGauze, ClothSilk, Confirm }

        AudioSource operation;
        public AudioSource music;
        AudioClip[] clips;
        const int Rate = 44100;

        void Awake()
        {
            I = this;
            operation = gameObject.AddComponent<AudioSource>();
            operation.playOnAwake = false;
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            clips = new AudioClip[System.Enum.GetValues(typeof(Cue)).Length];
            clips[(int)Cue.ShuttleSteady] = Tone("shuttle_steady", 0.06f, 1800f, 0.0f, 40f);
            clips[(int)Cue.ShuttleRough] = Tone("shuttle_rough", 0.14f, 900f, 0.5f, 18f);
            clips[(int)Cue.Break] = Tone("thread_break", 0.05f, 2600f, 0.2f, 60f);
            clips[(int)Cue.Needle] = Tone("needle", 0.04f, 3200f, 0.05f, 70f);
            clips[(int)Cue.NeedleDry] = Tone("needle_dry", 0.09f, 1400f, 0.6f, 30f);
            clips[(int)Cue.Water] = Tone("water", 0.35f, 300f, 0.8f, 8f);
            clips[(int)Cue.WaterBroken] = Tone("water_broken", 0.2f, 500f, 0.95f, 20f);
            clips[(int)Cue.Wood] = Tone("wood", 0.12f, 220f, 0.15f, 25f);
            clips[(int)Cue.ClothGauze] = Tone("cloth_gauze", 0.08f, 2400f, 0.9f, 35f);
            clips[(int)Cue.ClothSilk] = Tone("cloth_silk", 0.16f, 900f, 0.9f, 18f);
            clips[(int)Cue.Confirm] = Tone("confirm", 0.1f, 660f, 0.0f, 20f);
            ApplyVolumes();
            Settings.Changed += ApplyVolumes;
        }

        void OnDestroy()
        {
            Settings.Changed -= ApplyVolumes;
            if (I == this) I = null;
        }

        void ApplyVolumes()
        {
            operation.volume = Settings.Current.operationVolume;
            music.volume = Settings.Current.musicVolume;
        }

        public static void Play(Cue cue)
        {
            if (I == null) return;
            I.operation.PlayOneShot(I.clips[(int)cue]);
        }

        /// <summary>正弦加噪声的衰减短音。noise 0 为纯音，1 为纯噪声。</summary>
        static AudioClip Tone(string name, float seconds, float freq, float noise, float decay)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            var rng = new System.Random(name.GetHashCode());
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(i / 40f);
                float sine = Mathf.Sin(2f * Mathf.PI * freq * t);
                float white = (float)(rng.NextDouble() * 2 - 1);
                lp += (white - lp) * Mathf.Clamp01(freq / Rate * 6f);
                data[i] = env * 0.35f * Mathf.Lerp(sine, lp * 3f, noise);
            }
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
