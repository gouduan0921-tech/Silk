using System;
using System.Collections.Generic;
using System.Globalization;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>展柜与开幕（docs/09、docs/04 §10、§7）。展览只写展位与当日标记，不改成衣。</summary>
    public static class Exhibits
    {
        public const string ThemeYiSe = "yiSe";
        public const string ThemeCanToSi = "canToSi";
        public const int TitleMaxChars = 40; // docs/09 §3

        public static int Slots(ConfigSnapshot c) => c.balance.exhibit.launchSlots;

        public static Result Place(SaveRoot s, ConfigSnapshot c, int index, string kind, string itemId)
        {
            if (index < 0 || index >= Slots(c)) return Result.Fail("没有这个展位");
            if (kind == "garment" && Find.Garment(s, itemId) == null) return Result.Fail("没有这件成衣");
            if (kind == "bolt" && Find.Bolt(s, itemId) == null) return Result.Fail("没有这匹布");
            if (kind != "garment" && kind != "bolt") return Result.Fail("展位只放成衣或布");
            if (s.exhibit.slots.Exists(x => x.itemId == itemId)) return Result.Fail("这件已经在柜里");
            s.exhibit.slots.RemoveAll(x => x.index == index);
            s.exhibit.slots.Add(new ExhibitSlot { index = index, itemKind = kind, itemId = itemId, label = new ExhibitLabel { title = "" } });
            CheckTutorial3(s);
            return Result.Ok();
        }

        public static Result Remove(SaveRoot s, int index)
        {
            s.exhibit.slots.RemoveAll(x => x.index == index);
            return Result.Ok();
        }

        public static int CharCount(string text) => text == null ? 0 : new StringInfo(text).LengthInTextElements;

        /// <summary>说明牌：标题不超过 40 字；只能开关原料、染料层、是否首发朝代典型三项（docs/09 §3）。</summary>
        public static Result EditLabel(SaveRoot s, int index, string title, bool showMaterial, bool showDyeLayers, bool showDynasty)
        {
            var slot = s.exhibit.slots.Find(x => x.index == index);
            if (slot == null) return Result.Fail("展位是空的");
            if (CharCount(title) > TitleMaxChars) return Result.Fail("标题不超过 " + TitleMaxChars + " 字");
            slot.label = new ExhibitLabel { title = title ?? "", showMaterial = showMaterial, showDyeLayers = showDyeLayers, showDynasty = showDynasty };
            return Result.Ok();
        }

        public static void SetTheme(SaveRoot s, string theme) => s.exhibit.theme = theme;

        public static bool CanOpen(SaveRoot s, ConfigSnapshot c) => s.exhibit.slots.Count >= c.balance.exhibit.minItems;

        /// <summary>展览分（docs/04 §10）。</summary>
        public static double Score(SaveRoot s, ConfigSnapshot c)
        {
            var e = c.balance.exhibit;
            var slots = s.exhibit.slots;
            if (slots.Count == 0) return 0;
            double qSum = 0; int qN = 0, labelOn = 0, garments = 0, withImage = 0;
            var lastDyes = new Dictionary<string, int>();
            bool hasBolt = false, hasGarment = false, allMaterial = true;
            foreach (var slot in slots)
            {
                double? q = null;
                string lastDye = null;
                if (slot.itemKind == "garment")
                {
                    var g = Find.Garment(s, slot.itemId);
                    if (g == null) continue;
                    hasGarment = true;
                    garments++;
                    if (ItemQuality.TryGarmentQ(s, c, g.parts, out var gq, out _)) q = gq;
                    if (s.performances.Exists(p => p.garmentId == g.id)) withImage++;
                    var outer = g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer");
                    var b = outer != null ? Find.Bolt(s, outer.boltId) : null;
                    if (b != null && b.dyeLayers.Count > 0) lastDye = b.dyeLayers[b.dyeLayers.Count - 1].dyeId;
                }
                else
                {
                    var b = Find.Bolt(s, slot.itemId);
                    if (b == null) continue;
                    hasBolt = true;
                    q = ItemQuality.BoltQ(b, c);
                    if (b.dyeLayers.Count > 0) lastDye = b.dyeLayers[b.dyeLayers.Count - 1].dyeId;
                }
                if (q.HasValue) { qSum += q.Value; qN++; }
                var l = slot.label ?? new ExhibitLabel();
                labelOn += (l.showMaterial ? 1 : 0) + (l.showDyeLayers ? 1 : 0) + (l.showDynasty ? 1 : 0);
                if (!l.showMaterial) allMaterial = false;
                string key = lastDye ?? "undyed";
                lastDyes[key] = lastDyes.TryGetValue(key, out var n) ? n + 1 : 1;
            }
            double quality = qN > 0 ? qSum / qN : 0;
            double theme;
            if (s.exhibit.theme == ThemeCanToSi) theme = hasBolt && hasGarment && allMaterial ? 100 : 0;
            else
            {
                int best = 0;
                foreach (var kv in lastDyes) best = Math.Max(best, kv.Value);
                theme = best * 100.0 / slots.Count;
            }
            double label = labelOn * 100.0 / (slots.Count * 3);
            double image = garments > 0 ? withImage * 100.0 / garments : 0;
            return quality * e.quality + theme * e.theme + label * e.label + image * e.image;
        }

        /// <summary>
        /// 每日首次开馆结算（docs/09 §4、docs/21 §6）：不足开幕件数时只是单柜预览，不写结算日、不发丝钱（F5）；
        /// 同一天第二次结算为 0（N10）。
        /// </summary>
        public static int SettleToday(SaveRoot s, ConfigSnapshot c, out double score)
        {
            score = 0;
            if (!CanOpen(s, c)) return 0;
            if (s.exhibit.lastPaidDay == s.dayIndex) return 0;
            score = Score(s, c);
            var e = c.balance.economy;
            int coins = Math.Min(e.exhibitCoinCap, (int)Math.Floor(score / e.exhibitPointsPer) * e.exhibitCoinsPer);
            s.silkCoin += coins;
            s.exhibit.lastPaidDay = s.dayIndex;
            return coins;
        }

        /// <summary>教学第 3 步：西施穿上、完成 0 档、放入展柜（docs/10 §2）。</summary>
        public static void CheckTutorial3(SaveRoot s)
        {
            foreach (var slot in s.exhibit.slots)
            {
                if (slot.itemKind != "garment") continue;
                if (s.performances.Exists(p => p.garmentId == slot.itemId && p.characterId == "xiShi" && p.tier == 0))
                {
                    Craft.CompleteQuest(s, Craft.Tutorial3);
                    return;
                }
            }
        }
    }
}
