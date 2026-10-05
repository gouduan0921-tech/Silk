using System;
using System.Collections.Generic;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>
    /// 委托（docs/10 §2、§3）。教学委托固定，不被随机刷新替换；日常委托 3 级起按 hash(档 id, 日序) 刷新，读档不重掷。
    /// </summary>
    public static class Quests
    {
        /// <summary>按当天日序补足日常委托。同一档同一天调用多少次结果都一样。</summary>
        public static void Refresh(SaveRoot s, ConfigSnapshot c)
        {
            var r = c.balance.questRules;
            if (s.level < r.dailyFromLevel) return;
            if (s.quests.Exists(q => !q.tutorial && q.createdDay == s.dayIndex)) return; // 今天已刷过
            int held = s.quests.FindAll(q => !q.tutorial && !q.done && !q.expired).Count;
            int add = Math.Min(r.dailyCount, r.maxHeld - held);
            if (add <= 0) return;
            ulong h = StableHash.Of(s.rngSalt, s.dayIndex);
            var varieties = Unlocks.OpenVarieties(s, c);
            var dyes = Unlocks.OpenDyes(s, c);
            var chars = new List<string>();
            foreach (var ch in c.characters) if (Unlocks.CharacterOpen(s, c, ch.id, out _)) chars.Add(ch.id);
            for (int i = 0; i < add; i++)
            {
                ulong k = h >> (i * 16);
                s.quests.Add(new QuestState
                {
                    id = Ids.Next(s, "quest"),
                    tutorial = false,
                    needKind = "garment",
                    needVariety = varieties.Count > 0 ? varieties[(int)(k % (ulong)varieties.Count)] : null,
                    needDye = dyes.Count > 0 ? dyes[(int)((k >> 4) % (ulong)dyes.Count)] : null,
                    needMinTier = ((k >> 8) & 1) == 0 ? QualityCalc.Common : QualityCalc.Fine,
                    characterId = chars.Count > 0 ? chars[(int)((k >> 10) % (ulong)chars.Count)] : null,
                    createdDay = s.dayIndex,
                    expireDay = s.dayIndex + r.days,
                });
            }
        }

        public static void Expire(SaveRoot s)
        {
            foreach (var q in s.quests)
                if (!q.tutorial && !q.done && !q.expired && s.dayIndex >= q.expireDay) q.expired = true;
        }

        static int TierRank(string t) => t == QualityCalc.Legendary ? 2 : t == QualityCalc.Fine ? 1 : t == QualityCalc.Common ? 0 : -1;

        /// <summary>委托只检查成品字段，不检查中间失败次数（docs/10 §3）。</summary>
        public static bool Matches(SaveRoot s, QuestState q, Garment g)
        {
            if (g == null || g.tier == null) return false;
            if (TierRank(g.tier) < TierRank(q.needMinTier)) return false;
            var outer = g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer");
            var bolt = outer != null ? Find.Bolt(s, outer.boltId) : null;
            if (bolt == null) return false;
            if (q.needVariety != null && bolt.variety != q.needVariety) return false;
            if (q.needDye != null && (bolt.dyeLayers.Count == 0 || bolt.dyeLayers[bolt.dyeLayers.Count - 1].dyeId != q.needDye)) return false;
            return true;
        }

        /// <summary>交付：成衣从仓库移到委托记录；主题委托每角色一次加好感（docs/04 §6）。</summary>
        public static Result Deliver(SaveRoot s, ConfigSnapshot c, string questId, string garmentId)
        {
            var q = Find.Quest(s, questId);
            if (q == null || q.tutorial || q.done || q.expired) return Result.Fail("这张委托不能交付");
            var g = Find.Garment(s, garmentId);
            if (!Matches(s, q, g)) return Result.Fail("这件成衣不符合委托");
            q.done = true;
            q.deliveredItemId = g.id;
            s.exhibit.slots.RemoveAll(x => x.itemKind == "garment" && x.itemId == g.id);
            s.garments.Remove(g);
            if (q.characterId != null)
            {
                var ch = Find.Character(s, q.characterId);
                if (!ch.themeQuestRewarded)
                {
                    var ev = c.balance.affection.events.Find(e => e.key == "themeQuest");
                    if (ev != null) ch.affection = Math.Min(100, ch.affection + ev.value);
                    ch.themeQuestRewarded = true;
                    Stage.UpdateUnlockedTiers(s, c, ch.id);
                }
            }
            return Result.Ok();
        }
    }
}
