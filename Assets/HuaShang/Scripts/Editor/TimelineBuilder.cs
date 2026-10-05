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
            { "liQingZhao", new[] { "（李清照执卷立定）", "（她低头看衣襟的纹路）", "（定格）" } },
            { "yangGuiFei", new[] { "（杨贵妃缓步上前）", "（披帛垂在臂弯）", "（定格）" } },
            { "luoShen", new[] { "（洛神凌波而来）", "（披帛贴着水面拂过）", "（定格）" } },
            { "changE", new[] { "（嫦娥独立月下）", "（她抬头望月，袖口垂落）", "（定格）" } },
            { "baiSuZhen", new[] { "（白素贞撑伞立于湖边）", "（披帛末端微微一闪）", "（定格）" } },
            { "daJi", new[] { "（妲己款步上前，回眸）", "（衣上金线压着光）", "（定格）" } },
            { "haiLun", new[] { "（海伦立于城头）", "（长衣的竖褶一动不动）", "（定格）" } },
            { "keLiAoPaTeLa", new[] { "（克利奥帕特拉端坐，缓缓起身）", "（细褶贴身，一丝不乱）", "（定格）" } },
            { "xiaoYeXiaoTing", new[] { "（小野小町跪坐檐下）", "（她看着雨中的花）", "（定格）" } },
            { "aFuLuoDiTe", new[] { "（阿佛洛狄忒自海边走来）", "（肩上长巾被海风掀起一角）", "（定格）" } },
        };

        /// <summary>有转身的档位（30、70）发展段字幕；没写的角色用通用一句。</summary>
        static readonly Dictionary<string, string> TurnCaptions = new Dictionary<string, string>
        {
            { "wangZhaoJun", "（转身回望，衣摆随风扬起）" },
            { "zhaoFeiYan", "（轻旋一周，裙摆随风张开）" },
            { "liQingZhao", "（转身，袖口随风微扬）" },
            { "yangGuiFei", "（转身，披帛随风扬起）" },
            { "luoShen", "（回身，水纹随步散开）" },
            { "changE", "（转身，月华落满衣襟）" },
            { "baiSuZhen", "（回身，披帛如蛇游开，鳞光点点）" },
            { "daJi", "（旋身，花瓣绕身而起）" },
            { "haiLun", "（转身远望，长衣下摆随风摆开）" },
            { "aFuLuoDiTe", "（回身，海沫从脚边升起）" },
            { "keLiAoPaTeLa", "（转身，金沙绕身而起）" },
            { "xiaoYeXiaoTing", "（起身回望，袖口与后摆在风里慢慢展开）" },
        };

        /// <summary>70 档高潮字幕（docs/08 §2）；没写的角色用通用一句。</summary>
        static readonly Dictionary<string, string> Climax70 = new Dictionary<string, string>
        {
            { "xiShi", "（风起，广袖翻飞；无词短音）" },
            { "yangGuiFei", "（风起，披帛满台翻卷；无词短音）" },
            { "wangZhaoJun", "（风起，她迎风而立，衣摆向后扬开；无词短音）" },
            { "zhaoFeiYan", "（风起，她旋身，裙摆整圈张开；无词短音）" },
            { "liQingZhao", "（风过，衣襟与袖口微扬，她不动；无词短音）" },
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
            // 保存后按路径重新取资源：CreateInstance 出来的对象在后续导入时可能被替换，留着旧引用会在编辑器里变成空
            foreach (var e in list)
                e.timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(Folder + "/LS_" + e.characterId + "_t" + e.tier + ".playable");
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
            if (hasTurn && len.tier >= 70)
            {
                // 70 档：发展段侧风不超过 30 档上限，高潮到 6–9（取中），定格前降到余韵值（docs/04 §9）
                AddWind(wind, devStart, len.develop, (float)wl.tier30WindMax, Vector3.right);
                AddWind(wind, climaxStart, len.climax, (float)((wl.tier70PeakMin + wl.tier70PeakMax) * 0.5), new Vector3(1, 0, -0.4f).normalized);
            }
            else if (hasTurn) AddWind(wind, devStart, codaStart - devStart, (float)wl.tier30WindMax, Vector3.right);
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
                Marker(camTrack, len.setup * 0.5, hasTurn ? (TurnCaptions.TryGetValue(len.characterId, out var turn) ? turn : "（转身，袖随风起）") : caps[1]);
                if (hasTurn) Marker(camTrack, climaxStart + 0.5, len.tier >= 70 ? (Climax70.TryGetValue(len.characterId, out var c70) ? c70 : "（风起，衣袂翻飞；无词短音）") : "（行至台前）");
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
