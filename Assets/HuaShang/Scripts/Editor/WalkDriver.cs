using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Stations;
using HuaShang.UI;

namespace HuaShang.EditorTools
{
    /// <summary>
    /// 编辑器走查用：在暂停的 Play 模式里逐帧推进，读出木牌与侧架，按按钮。
    /// 只给开发者与自动化走查用，不进玩家版本（Editor 程序集）。
    /// </summary>
    public static class WalkDriver
    {
        public static WorkshopController W => Object.FindFirstObjectByType<WorkshopController>();

        /// <summary>固定 1/60 秒推进 n 帧（编辑器在后台时玩家循环不走，用 Step 推）。</summary>
        public static void Step(int frames)
        {
            Time.captureDeltaTime = 1f / 60f;
            if (!EditorApplication.isPaused) EditorApplication.isPaused = true;
            for (int i = 0; i < frames; i++) EditorApplication.Step();
        }

        public static void Seconds(float s) => Step(Mathf.CeilToInt(s * 60f));

        public static List<RackItem> Rack()
        {
            var f = typeof(Hud).GetField("rackItems", BindingFlags.NonPublic | BindingFlags.Instance);
            return (List<RackItem>)f.GetValue(Hud.I);
        }

        public static string Primary(int frames = 20)
        {
            var p = Hud.I.CurrentPlaque;
            if (p.onPrimary == null || !p.primaryEnabled) return "主按钮不可用：" + p.primaryLabel;
            p.onPrimary();
            Step(frames);
            return Dump();
        }

        public static string Secondary(string label, int frames = 20)
        {
            var p = Hud.I.CurrentPlaque;
            var s = p.secondary.Find(x => x.Key.Contains(label));
            if (s.Value == null) return "没有次按钮：" + label;
            s.Value();
            Step(frames);
            return Dump();
        }

        public static string Option(string label, int index, int frames = 2)
        {
            var o = Hud.I.CurrentPlaque.options.Find(x => x.label.Contains(label));
            if (o == null) return "没有选项：" + label;
            o.selected = index; // 与界面点选一致
            o.onSelect?.Invoke(index);
            Step(frames);
            return Dump();
        }

        public static string Pick(string id, int frames = 2)
        {
            var it = Rack().Find(x => x.id == id);
            if (it == null || it.onClick == null) return "侧架没有可点的：" + id;
            it.onClick();
            Step(frames);
            return Dump();
        }

        public static string Beats(int count, float offsetSeconds = 0f)
        {
            // 每拍目标点之后 offsetSeconds 按空格
            float interval = (float)GameSession.I.Config.balance.weave.beatInterval;
            float t0 = Time.time;
            int pressed = 0;
            int guard = Mathf.CeilToInt((count + 2) * interval * 60f) + 10;
            for (int i = 0; i < guard && pressed < count; i++)
            {
                Step(1);
                if (Time.time - t0 >= interval * (pressed + 1) + offsetSeconds) { W.current.BeatKey(); pressed++; }
            }
            Step(2);
            return Dump();
        }

        /// <summary>新开一局，按教学路线走到人台收成襦裙（稳拍、中浓、温水、6 秒起布）。返回每站完成时的木牌。</summary>
        public static string Day1ToGarment()
        {
            var log = new StringBuilder();
            Step(5);
            GameSession.I.StartNewGame();
            Step(5);
            var w = W;
            w.Approach(w.stations.Find(s => s.stationId == "station_loom"), true);
            Step(5);
            log.Append(Primary()).Append("\n");          // 确认开局绢
            Primary(5); Primary(40);                        // 走去染缸、进入
            Pick("indigo");
            Primary(1);
            float center = (float)GameSession.I.Config.balance.dye.LiftCenterOf(HuaShang.Rules.Calc.DyeCalc.Medium);
            float interval = (float)GameSession.I.Config.balance.weave.beatInterval;
            Beats(Mathf.FloorToInt(center / interval));
            Seconds(center - interval * Mathf.FloorToInt(center / interval));
            log.Append(Primary()).Append("\n");          // 起布
            Primary(5); Primary(40);                        // 裁桌
            Primary(10); Primary(10);                       // 上襦、裙
            Secondary("针线", 5); Primary(40);
            Primary(1); Beats(GameSession.I.Config.balance.weave.sewNeedlesPerPart); Primary(5);
            Primary(1); Beats(GameSession.I.Config.balance.weave.sewNeedlesPerPart);
            log.Append(Primary(5)).Append("\n");
            Primary(5); Primary(90);                        // 人台
            log.Append(Primary(30)).Append("\n");        // 收成襦裙
            return log.ToString();
        }

        public static string Dump()
        {
            var sb = new StringBuilder();
            var w = W;
            var p = Hud.I.CurrentPlaque;
            var s = GameSession.I.Save;
            sb.Append("[" + (w.current ? w.current.stationId : "-") + " " + w.layer + "] 日" + s.dayIndex + " 工时" + s.hoursUsed + " 丝钱" + s.silkCoin + " 等级" + s.level + " xp" + s.xp + "\n");
            if (p != null)
            {
                sb.Append(p.kicker + " | " + p.title + " | " + p.body + "\n");
                sb.Append(" 主=" + p.primaryLabel + (p.primaryEnabled ? "" : "(灰)") + (p.resultTier != null ? " 档=" + p.resultTier : "") + (p.source != null ? " 来源=" + p.source : "") + "\n");
                if (p.traits.Count > 0) sb.Append(" 词条=" + string.Join("、", p.traits) + "\n");
                if (!string.IsNullOrEmpty(p.details)) sb.Append(" 详情=" + p.details.Replace("\n", " / ") + "\n");
                foreach (var o in p.options) sb.Append(" 选 " + o.label + ": " + string.Join("/", o.choices) + " [" + o.selected + "]\n");
                foreach (var t in p.toggles) sb.Append(" 开关 " + t.label + "=" + t.on + "\n");
                foreach (var x in p.secondary) sb.Append(" 次 " + x.Key + "\n");
                foreach (var x in p.warnings) sb.Append(" ⚠ " + x + "\n");
            }
            foreach (var it in Rack()) sb.Append(" 架 " + it.kind + ":" + it.id + " " + it.label + " · " + it.sub + (it.selected ? " ✓" : "") + (it.interactable ? "" : " (灰)") + "\n");
            return sb.ToString();
        }
    }
}
