using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using HuaShang.Game;
using HuaShang.Performance;
using HuaShang.Rules.Config;

namespace HuaShang.EditorTools
{
    /// <summary>
    /// 按 docs/04 §9 的段落长度生成首发四条时间轴（Assets/HuaShang/Stages）。一条时间轴四条轨道：
    /// 动画、MagicaWindZone、灯、摄像机（docs/08 §3）。0 档没有转身事件就不挂侧风。
    /// 灰盒动作是根节点的转身与起伏；西施动画交付后替换动画片段，其余三条轨道不变。
    /// </summary>
    public static class TimelineBuilder
    {
        public const string Folder = "Assets/HuaShang/Stages";

        static readonly Dictionary<string, string[]> Captions = new Dictionary<string, string[]>
        {
            { "xiShi", new[] { "（西施立定，缓缓抬袖）", "（袖口在风里轻轻浮起）", "（定格）" } },
            { "wangZhaoJun", new[] { "（王昭君立定，衣摆垂直）", "（她回望远处）", "（定格）" } },
            { "zhaoFeiYan", new[] { "（赵飞燕轻步站定）", "（裙摆随步子收回）", "（定格）" } },
        };

        [MenuItem("HuaShang/演出/生成首发时间轴")]
        public static List<TimelineEntry> BuildAll()
        {
            var c = GameConfig.LoadInEditor();
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/HuaShang", "Stages");
            var list = new List<TimelineEntry>();
            foreach (var t in c.balance.windLight.timelines)
            {
                var row = c.characters.Find(x => x.id == t.characterId);
                if (row == null || !row.enabled) continue; // 关闭角色不做时间轴
                list.Add(new TimelineEntry { characterId = t.characterId, tier = t.tier, timeline = Build(c, t) });
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[HuaShang] 已生成 " + list.Count + " 条时间轴");
            return list;
        }

        static TimelineAsset Build(ConfigSnapshot c, TimelineLength len)
        {
            string name = "LS_" + len.characterId + "_t" + len.tier; // docs/17 §1
            string path = Folder + "/" + name + ".playable";
            AssetDatabase.DeleteAsset(path);
            var tl = ScriptableObject.CreateInstance<TimelineAsset>();
            tl.name = name;
            AssetDatabase.CreateAsset(tl, path);
            double total = len.Total;
            double devStart = len.setup, climaxStart = len.setup + len.develop, codaStart = climaxStart + len.climax;
            bool hasTurn = len.develop > 0; // 0 档只有铺垫与余韵，没有转身
            var wl = c.balance.windLight;
            float baseWind = (float)((wl.baseWindMin + wl.baseWindMax) * 0.5);

            // 动画
            var clip = BodyClip(name, len, hasTurn);
            AssetDatabase.AddObjectToAsset(clip, tl);
            var anim = tl.CreateTrack<AnimationTrack>(null, "动画");
            anim.trackOffset = TrackOffset.ApplySceneOffsets;
            var ac = anim.CreateClip(clip);
            ac.start = 0; ac.duration = total;

            // 风：基础风常在；转身时侧风（不超过 30 档上限）；定格前降到余韵值（docs/04 §9、docs/08 §3）
            var wind = tl.CreateTrack<WindTrack>(null, "MagicaWindZone");
            AddWind(wind, 0, hasTurn ? devStart : codaStart, baseWind, new Vector3(0, 0, -1));
            if (hasTurn) AddWind(wind, devStart, codaStart - devStart, (float)wl.tier30WindMax, Vector3.right);
            AddWind(wind, codaStart, total - codaStart, (float)wl.tier70EndMax, new Vector3(0, 0, -1));

            // 灯：0 档一盏柔光；30 档转身时加边缘光，定格前回到轮廓
            var light = tl.CreateTrack<LightTrack>(null, "灯");
            var neutral = wl.colorTemps.Find(x => x.label == "素");
            float kelvin = neutral != null ? neutral.kelvin : 5200;
            AddLight(light, 0, hasTurn ? devStart : codaStart, 1.0f, kelvin, 0f);
            if (hasTurn) AddLight(light, devStart, codaStart - devStart, 1.2f, kelvin, 1.6f);
            AddLight(light, codaStart, total - codaStart, 0.9f, kelvin, hasTurn ? 0.6f : 0f);

            // 摄像机：0 档一条路径，不快切（docs/15 §4、docs/08 §5）
            var camTrack = tl.CreateTrack<CameraPoseTrack>(null, "摄像机");
            if (!hasTurn)
            {
                AddCam(camTrack, 0, total, new Vector3(0.3f, 1.55f, -4.4f), new Vector3(0.1f, 1.4f, -3.3f), 34f);
            }
            else
            {
                AddCam(camTrack, 0, devStart, new Vector3(0, 1.55f, -4.4f), new Vector3(0, 1.5f, -3.8f), 36f);
                AddCam(camTrack, devStart, len.develop, new Vector3(-1.6f, 1.5f, -3.4f), new Vector3(1.6f, 1.45f, -3.2f), 36f);
                AddCam(camTrack, climaxStart, len.climax, new Vector3(0.8f, 1.4f, -3.0f), new Vector3(0.3f, 1.35f, -2.5f), 32f);
                AddCam(camTrack, codaStart, total - codaStart, new Vector3(0.2f, 1.45f, -3.2f), new Vector3(0.1f, 1.4f, -3.0f), 32f);
            }

            // 字幕：写动作（docs/18 §1）
            if (Captions.TryGetValue(len.characterId, out var caps))
            {
                Marker(camTrack, 0.5, caps[0]);
                Marker(camTrack, len.setup * 0.5, hasTurn ? "（转身，袖随风起）" : caps[1]);
                if (hasTurn) Marker(camTrack, climaxStart + 0.5, "（行至台前）");
                Marker(camTrack, codaStart + 0.3, caps[2]);
            }
            EditorUtility.SetDirty(tl);
            return tl;
        }

        static AnimationClip BodyClip(string name, TimelineLength len, bool hasTurn)
        {
            var clip = new AnimationClip { name = name + "_body" };
            double total = len.Total, devStart = len.setup, climaxStart = len.setup + len.develop, codaStart = climaxStart + len.climax;
            var yaw = new AnimationCurve();
            var z = new AnimationCurve();
            var y = new AnimationCurve();
            yaw.AddKey(0, 0);
            if (!hasTurn)
            {
                yaw.AddKey((float)(len.setup * 0.35), 12f);
                yaw.AddKey((float)(len.setup * 0.7), -10f);
                yaw.AddKey((float)codaStart, 0f);
            }
            else
            {
                yaw.AddKey((float)(devStart * 0.6), 10f);
                yaw.AddKey((float)devStart, 0f);
                yaw.AddKey((float)(devStart + 1.6), 180f);
                yaw.AddKey((float)(devStart + len.develop * 0.6), 180f);
                yaw.AddKey((float)(devStart + len.develop * 0.6 + 1.6), 360f);
                yaw.AddKey((float)codaStart, 360f);
            }
            yaw.AddKey((float)total, hasTurn ? 360f : 0f);
            z.AddKey(0, 0);
            z.AddKey((float)climaxStart, 0);
            if (hasTurn) z.AddKey((float)codaStart, -0.6f);
            z.AddKey((float)total, hasTurn ? -0.6f : 0f);
            for (float t = 0; t <= total; t += 1.5f) y.AddKey(t, t < codaStart ? Mathf.Sin(t * 1.3f) * 0.012f : 0f);
            y.AddKey((float)total, 0f);
            var zero = AnimationCurve.Constant(0, (float)total, 0f);
            Set(clip, "localEulerAnglesRaw.x", zero);
            Set(clip, "localEulerAnglesRaw.y", yaw);
            Set(clip, "localEulerAnglesRaw.z", zero);
            Set(clip, "m_LocalPosition.x", zero);
            Set(clip, "m_LocalPosition.y", y);
            Set(clip, "m_LocalPosition.z", z);
            return clip;
        }

        static void Set(AnimationClip clip, string prop, AnimationCurve curve)
            => AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), prop), curve);

        static void AddWind(WindTrack track, double start, double duration, float speed, Vector3 dir)
        {
            if (duration <= 0) return;
            var tc = track.CreateClip<WindClip>();
            tc.start = start; tc.duration = duration; tc.displayName = "风 " + speed.ToString("0.0");
            var a = (WindClip)tc.asset;
            a.template.speed = speed;
            a.template.direction = dir;
        }

        static void AddLight(LightTrack track, double start, double duration, float key, float kelvin, float rim)
        {
            if (duration <= 0) return;
            var tc = track.CreateClip<LightClip>();
            tc.start = start; tc.duration = duration; tc.displayName = rim > 0 ? "主光 + 边缘光" : "柔光";
            var a = (LightClip)tc.asset;
            a.template.keyIntensity = key;
            a.template.kelvin = kelvin;
            a.template.rimIntensity = rim;
        }

        static void AddCam(CameraPoseTrack track, double start, double duration, Vector3 from, Vector3 to, float fov)
        {
            if (duration <= 0) return;
            var tc = track.CreateClip<CameraPoseClip>();
            tc.start = start; tc.duration = duration; tc.displayName = "机位";
            var a = (CameraPoseClip)tc.asset;
            a.template.fromPosition = from;
            a.template.toPosition = to;
            a.template.lookAt = new Vector3(0, 1.15f, 0);
            a.template.fov = fov;
        }

        static void Marker(TrackAsset track, double time, string text)
        {
            var m = track.CreateMarker<SubtitleMarker>(time);
            m.text = text;
        }
    }
}
