using System.Collections.Generic;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>
    /// 季节轮转（docs/04 §3）：3 级前恒为春；达到 3 级后的下一个工坊日起，这天是春的第 1 日，之后每 6 个工坊日一换。
    /// 换季时没用完的新鲜染料晒成同种干料，份数不变。季节不改写已经下机的布（docs/02 §2）。
    /// </summary>
    public static class Seasons
    {
        public static SeasonRow Current(SaveRoot s, ConfigSnapshot c) => c.balance.season.Row(s.season);

        /// <summary>按日序算当季 id。</summary>
        public static string SeasonAt(SaveRoot s, ConfigSnapshot c, int dayIndex)
        {
            var rows = c.balance.season.rows;
            if (!s.seasonStartDay.HasValue || dayIndex < s.seasonStartDay.Value || rows.Count == 0) return rows.Count > 0 ? rows[0].id : s.season;
            int n = (dayIndex - s.seasonStartDay.Value) / c.balance.season.daysPerSeason;
            return rows[n % rows.Count].id;
        }

        /// <summary>当季第几天（从 1 起）；未轮转时为空。</summary>
        public static int? DayOfSeason(SaveRoot s, ConfigSnapshot c)
        {
            if (!s.seasonStartDay.HasValue) return null;
            return (s.dayIndex - s.seasonStartDay.Value) % c.balance.season.daysPerSeason + 1;
        }

        /// <summary>新的一天开始时调用（日序已加一）。换季返回一句提示，否则为空。</summary>
        public static string Advance(SaveRoot s, ConfigSnapshot c)
        {
            if (!s.seasonStartDay.HasValue && s.level >= c.balance.season.rotationFromLevel)
                s.seasonStartDay = s.dayIndex;
            string next = SeasonAt(s, c, s.dayIndex);
            if (next == s.season) return null;
            s.season = next;
            int dried = 0;
            foreach (var d in new List<DyeStock>(s.dyes))
            {
                if (d.state != Find.Fresh) continue;
                dried += d.count;
                var dry = Find.Dye(s, d.dyeId);
                if (dry == null) { d.state = Find.Dry; continue; }
                dry.count += d.count;
                s.dyes.Remove(d);
            }
            var row = Current(s, c);
            string label = row != null ? row.label : next;
            return "入" + label + "。" + (dried > 0 ? "没用完的新鲜染料晒成了干料（" + dried + " 份）。" : "") + (row != null && !row.CanHatch ? "冬天不结新茧。" : "");
        }

        /// <summary>当季市集上的新鲜染料：本季的、且等级已开放的（docs/04 §3）。</summary>
        public static List<string> FreshOnSale(SaveRoot s, ConfigSnapshot c)
        {
            var list = new List<string>();
            var row = Current(s, c);
            if (row == null || !s.seasonStartDay.HasValue) return list; // 轮转开始前不卖新鲜染料
            foreach (var id in row.freshDyes)
                if (Unlocks.DyeOpen(s, c, id)) list.Add(id);
            return list;
        }
    }
}
