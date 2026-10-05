using System;
using System.Collections.Generic;
using System.IO;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using HuaShang.Game;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Save;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Performance
{
    [Serializable]
    public class TimelineEntry
    {
        public string characterId;
        public int tier;
        public TimelineAsset timeline;
    }

    /// <summary>
    /// 演出装配（docs/08）：角色、档位、成衣拷贝、舞台、呈现模式、时间轴。
    /// 演出中隐藏木牌与侧架，只留暂停；暂停真正停住时间轴与布料。播完才写演出记录（docs/21 §6）。
    /// 0 档没有自由相机；30 档自由相机限制在舞台盒子内，不能穿进布料（docs/08 §5）。
    /// </summary>
    public class PerformanceDirector : MonoBehaviour, INotificationReceiver
    {
        public PlayableDirector director;
        public Transform stageRoot;
        public GreyboxBody performer;
        public GarmentVisual visual;
        public MagicaWindZone stageWind;
        public StageLights lights;
        public Camera cam;
        public List<TimelineEntry> timelines = new List<TimelineEntry>();
        public Bounds freeCameraBox = new Bounds(new Vector3(0, 1.4f, -2.2f), new Vector3(5f, 2.4f, 3.6f));

        public bool Playing { get; private set; }
        public bool Paused { get; private set; }
        public Action<string> onFinished; // 演出记录 id

        string characterId, garmentId, shownMode;
        int tier;
        int cameraPreset;
        bool freeCam;
        Vector2 freeAngles;

        public bool HasTimeline(string ch, int t) => Find(ch, t) != null;

        TimelineAsset Find(string ch, int t)
        {
            var e = timelines.Find(x => x.characterId == ch && x.tier == t);
            return e != null ? e.timeline : null;
        }

        public void Play(string ch, int t, string gid, string presentMode, float lockedKelvin)
        {
            var s = GameSession.I.Save;
            var c = GameSession.I.Config;
            var g = HuaShang.Play.Find.Garment(s, gid);
            var tl = Find(ch, t);
            if (g == null || tl == null) return;
            characterId = ch; tier = t; garmentId = gid; shownMode = presentMode;
            visual.ownerId = ch;
            visual.BuildGarment(s, c, g, presentMode); // 呈现模式只改显示拷贝（docs/03 §2）
            if (ClothQualityDirector.Instance != null) ClothQualityDirector.Instance.stageFocusOwner = ch; // 同时只有一位角色用高档

            LightMixer.lockedKelvin = lockedKelvin;
            LightMixer.recommendedKelvin = RecommendedKelvin(s, c, g);
            CameraPoseMixer.stageRoot = stageRoot;
            CameraPoseMixer.overridden = false;
            cameraPreset = 0;
            freeCam = false;

            director.playableAsset = tl;
            foreach (var output in tl.outputs)
            {
                var track = output.sourceObject as TrackAsset;
                if (track is AnimationTrack) director.SetGenericBinding(track, performer.GetComponent<Animator>());
                else if (track is WindTrack) director.SetGenericBinding(track, stageWind);
                else if (track is LightTrack) director.SetGenericBinding(track, lights);
                else if (track is CameraPoseTrack) director.SetGenericBinding(track, cam);
            }
            director.extrapolationMode = DirectorWrapMode.None;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            director.stopped -= OnStopped;
            director.stopped += OnStopped;
            Playing = true;
            Paused = false;
            Hud.I.SetPerformanceMode(true, TogglePause);
            Hud.I.Subtitle(null);
            director.time = 0;
            director.Play();
        }

        /// <summary>推荐主光色温：按成衣外层最后一层染料的色系（docs/15 §3、docs/04 §9）。</summary>
        public static float RecommendedKelvin(SaveRoot s, Rules.Config.ConfigSnapshot c, Garment g)
        {
            var temps = c.balance.windLight.colorTemps;
            float K(string label) { var t = temps.Find(x => x.label == label); return t != null ? t.kelvin : 0; }
            var outer = g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer");
            var b = outer != null ? HuaShang.Play.Find.Bolt(s, outer.boltId) : null;
            string dye = b != null && b.dyeLayers.Count > 0 ? b.dyeLayers[b.dyeLayers.Count - 1].dyeId : null;
            switch (dye)
            {
                case "indigo": return K("蓝");
                case "madder": case "safflower": case "sappan": return K("红");
                case "gardenia": case "pagoda": return K("黄");
                default: return K("素");
            }
        }

        public void TogglePause()
        {
            if (!Playing) return;
            Paused = !Paused;
            Time.timeScale = Paused ? 0f : 1f; // 时间轴（GameTime）与布料一起停住
            AudioListener.pause = Paused;
            Hud.I.SetPausedLabel(Paused);
        }

        void Update()
        {
            if (!Playing) return;
            var kb = Keyboard.current;
            if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)) TogglePause();
            if (tier >= 30 && !Paused)
            {
                if (kb != null && kb.cKey.wasPressedThisFrame) { cameraPreset = (cameraPreset + 1) % 3; CameraPoseMixer.overridden = cameraPreset != 0; freeCam = false; }
                var mouse = Mouse.current;
                if (mouse != null && mouse.rightButton.isPressed)
                {
                    freeCam = true;
                    CameraPoseMixer.overridden = true;
                    freeAngles += mouse.delta.ReadValue() * 0.2f;
                }
            }
        }

        void LateUpdate()
        {
            if (!Playing || tier < 30 || !CameraPoseMixer.overridden) return;
            Vector3 focus = performer.transform.position + Vector3.up * 1.1f;
            Vector3 local;
            if (freeCam)
            {
                var rot = Quaternion.Euler(Mathf.Clamp(-freeAngles.y, -10f, 35f), freeAngles.x + 180f, 0);
                local = stageRoot.InverseTransformPoint(focus + rot * Vector3.forward * 2.6f);
            }
            else local = cameraPreset == 1 ? new Vector3(1.6f, 1.5f, -2.2f) : new Vector3(-1.4f, 0.8f, -1.8f);
            // 限制在舞台盒子内
            local = new Vector3(Mathf.Clamp(local.x, freeCameraBox.min.x, freeCameraBox.max.x),
                                Mathf.Clamp(local.y, freeCameraBox.min.y, freeCameraBox.max.y),
                                Mathf.Clamp(local.z, freeCameraBox.min.z, freeCameraBox.max.z));
            Vector3 world = stageRoot.TransformPoint(local);
            // 不穿进布料：离身体胶囊至少留出布的外扩距离
            const float clearance = 0.6f;
            for (int i = 0; i < 8 && performer.SignedDistance(world) < clearance; i++)
                world += (world - focus).normalized * 0.1f;
            cam.transform.position = world;
            cam.transform.rotation = Quaternion.LookRotation(focus - world, Vector3.up);
        }

        void OnStopped(PlayableDirector d)
        {
            if (!Playing) return;
            Playing = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            CameraPoseMixer.overridden = false;
            Hud.I.Subtitle(null);
            Hud.I.SetPerformanceMode(false, null);
            if (ClothQualityDirector.Instance != null) ClothQualityDirector.Instance.stageFocusOwner = null;
            // 演出完成才写记录（docs/21 §6）
            var r = GameSession.I.Run((s, c) => Stage.Record(s, c, characterId, tier, garmentId, shownMode));
            if (r.ok) SaveSnapshot(r.createdId);
            else Hud.I.Toast(r.error);
            onFinished?.Invoke(r.ok ? r.createdId : null);
        }

        /// <summary>中途放弃：不写记录。</summary>
        public void Abort()
        {
            if (!Playing) return;
            Playing = false;
            director.Stop();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Hud.I.SetPerformanceMode(false, null);
        }

        /// <summary>定格图存在本机演出记录里（docs/24 §1）。</summary>
        static void SaveSnapshot(string perfId)
        {
            try
            {
                string dir = Path.Combine(Application.persistentDataPath, "snapshots");
                Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(Path.Combine(dir, perfId + ".png"));
            }
            catch (Exception e) { Debug.LogWarning("[HuaShang] 定格图未保存：" + e.Message); }
        }

        public void OnNotify(Playable origin, INotification notification, object context)
        {
            if (notification is SubtitleMarker m) Hud.I.Subtitle(m.text);
        }
    }
}
