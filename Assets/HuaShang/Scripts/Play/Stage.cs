using System;
using System.Collections.Generic;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>
    /// 演出的选人、契合、热度与好感（docs/08、docs/16、docs/04 §6）。演出只写演出记录与好感，不改仓库原件。
    /// </summary>
    public static class Stage
    {
        /// <summary>某角色当前可选的档位。0 档由等级打开；30、70 档要好感到门槛且不超过 launchTierMax（docs/05 §5）。</summary>
        public static List<int> AvailableTiers(SaveRoot s, ConfigSnapshot c, string characterId)
        {
            var list = new List<int>();
            var row = c.characters.Find(x => x.id == characterId);
            if (row == null || !row.enabled) return list;
            if (!Unlocks.CharacterOpen(s, c, characterId, out var levelTiers)) return list;
            int affection = Find.Character(s, characterId).affection;
            foreach (int tier in c.balance.affection.tierThresholds)
            {
                if (tier > row.launchTierMax) continue;
                if (levelTiers == null || !levelTiers.Contains(tier)) continue;
                if (affection >= tier) list.Add(tier);
            }
            return list;
        }

        /// <summary>上场门只显示有可选档位、且有对应时间轴的角色（docs/17 §2）。</summary>
        public static List<string> StageDoor(SaveRoot s, ConfigSnapshot c, Func<string, int, bool> hasTimeline)
        {
            var list = new List<string>();
            foreach (var ch in c.characters)
            {
                var tiers = AvailableTiers(s, c, ch.id);
                if (tiers.Count == 0) continue;
                if (hasTimeline != null && !tiers.Exists(t => hasTimeline(ch.id, t))) continue;
                list.Add(ch.id);
            }
            return list;
        }

        /// <summary>
        /// 契合（docs/04 §6）：品种命中、朝代或文化圈命中、词条命中、透明度或层数符合备注；四项都没有为底分。
        /// 首发品种表上的代计规则见 docs/16 §2（N9）。
        /// </summary>
        public static int Fit(SaveRoot s, ConfigSnapshot c, Garment g, string characterId)
        {
            var a = c.balance.affection;
            var row = c.characters.Find(x => x.id == characterId);
            if (row == null || g == null) return a.fitBase;
            var outer = g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer");
            var bolt = outer != null ? Find.Bolt(s, outer.boltId) : null;
            int fit = 0;
            if (bolt != null && VarietyHit(c, row, bolt.variety, s)) fit += a.fitVariety;
            if (!string.IsNullOrEmpty(row.preferDynasty) && row.preferDynasty == g.dynastyStyle) fit += a.fitDynasty;
            return fit == 0 ? a.fitBase : fit;
        }

        /// <summary>代计只在被代的品种对这位玩家还不可选时成立；s 为空时按表的开放列判断（docs/16 §2）。</summary>
        public static bool VarietyHit(ConfigSnapshot c, CharacterRow row, string varietyId, SaveRoot s = null)
        {
            if (row.preferVariety.Contains(varietyId)) return true;
            foreach (var sub in row.substitutes)
            {
                var missing = c.varieties.Find(v => v.id == sub.whenMissing);
                bool selectable = missing != null && missing.launch && (s == null || Unlocks.VarietyOpen(s, c, missing.id));
                if (!selectable && sub.countsAs == varietyId && row.preferVariety.Contains(sub.whenMissing)) return true;
            }
            return false;
        }

        /// <summary>热度 = Q×w + 契合×w + 阶段分×w（docs/04 §6）。</summary>
        public static double Heat(double q, int fit, int tier, ConfigSnapshot c)
        {
            var a = c.balance.affection;
            var st = a.stageScores.Find(x => x.tier == tier);
            return q * a.heatQ + fit * a.heatFit + (st != null ? st.score : 0) * a.heatStage;
        }

        public static int AffectionGain(int fit, ConfigSnapshot c)
        {
            var a = c.balance.affection;
            string key = fit >= a.fitHighMin ? "fitHigh" : fit >= a.fitMidMin ? "fitMid" : "fitLow";
            var ev = a.events.Find(e => e.key == key);
            return ev != null ? ev.value : 0;
        }

        public static void UpdateUnlockedTiers(SaveRoot s, ConfigSnapshot c, string characterId)
        {
            var st = Find.Character(s, characterId);
            st.unlockedTiers = AvailableTiers(s, c, characterId);
        }

        /// <summary>演出播完才调用：写演出记录与好感（docs/21 §6）。</summary>
        /// <param name="shownMode">这一场实际用的呈现模式；为空时取成衣覆盖或根设置（docs/21 §7）。</param>
        public const string StageClassic = "stage_classic", StageInk = "stage_ink";

        public static Result Record(SaveRoot s, ConfigSnapshot c, string characterId, int tier, string garmentId, string shownMode = null, string stageId = null)
        {
            var g = Find.Garment(s, garmentId);
            if (g == null) return Result.Fail("没有这件成衣");
            if (!AvailableTiers(s, c, characterId).Contains(tier)) return Result.Fail("这一档还没打开");
            ItemQuality.TryGarmentQ(s, c, g.parts, out double q, out _);
            int fit = Fit(s, c, g, characterId);
            bool replay = s.performances.Exists(p => p.garmentId == g.id);
            bool replayToday = s.performances.Exists(p => p.garmentId == g.id && p.dayIndex == s.dayIndex);
            var rec = new PerformanceRecord
            {
                id = Ids.Next(s, "perf"),
                characterId = characterId,
                tier = tier,
                garmentId = g.id,
                presentMode = shownMode ?? g.presentMode ?? s.presentMode,
                stageId = stageId ?? StageClassic,
                heat = Math.Round(Heat(q, fit, tier, c), 1),
                fit = fit,
                dayIndex = s.dayIndex,
            };
            s.performances.Add(rec);

            var ch = Find.Character(s, characterId);
            int gain = AffectionGain(fit, c);
            if (replay && !replayToday && g.tier == QualityCalc.Legendary)
            {
                var ev = c.balance.affection.events.Find(e => e.key == "legendaryReplay");
                if (ev != null) gain += ev.value;
            }
            ch.affection = Math.Min(100, ch.affection + gain); // 好感 0–100，不下降
            UpdateUnlockedTiers(s, c, characterId);
            Exhibits.CheckTutorial3(s);
            return Result.Ok(rec.id);
        }
    }
}
