using System.Collections.Generic;
using System.Text;
using UnityEngine;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>界面上的中文称呼与侧架实物。只读存档，不改数据。</summary>
    public static class Names
    {
        static readonly string[] DayNumerals = { "一", "二", "三", "四", "五", "六", "七", "八", "九", "十" };

        public static string Day(int dayIndex) => dayIndex < 10 ? "第" + DayNumerals[dayIndex] + "日" : "第" + (dayIndex + 1) + "日";

        public static string Season(string s) => s == "summer" ? "夏" : s == "autumn" ? "秋" : s == "winter" ? "冬" : "春";

        public static string Variety(ConfigSnapshot c, string id) => c.varieties.Find(v => v.id == id)?.name ?? id;
        public static string Dye(ConfigSnapshot c, string id) => c.dyes.Find(d => d.id == id)?.name ?? id;
        public static string Character(ConfigSnapshot c, string id) => c.characters.Find(x => x.id == id)?.name ?? id;

        public static string Slot(string slot)
        {
            switch (slot)
            {
                case "upper": return "上襦";
                case "skirt": return "裙";
                case "drape": return "披帛";
                case "inner": return "衬里";
                default: return slot;
            }
        }

        public static string Tier(string tier)
        {
            switch (tier)
            {
                case QualityCalc.Common: return "普通";
                case QualityCalc.Fine: return "精良";
                case QualityCalc.Legendary: return "传世";
                default: return "待评定";
            }
        }

        public static string Fineness(string f) => f == "fine" ? "细" : f == "coarse" ? "粗" : "中";

        public static string Bolt(ConfigSnapshot c, Bolt b) => b.id == NewGameFactory.OpeningBoltId ? Variety(c, b.variety) + " A" : Variety(c, b.variety);

        public static string Layers(ConfigSnapshot c, Bolt b)
        {
            if (b.dyeLayers.Count == 0) return "未染";
            var sb = new StringBuilder();
            var counts = new Dictionary<string, int>();
            foreach (var l in b.dyeLayers) counts[l.dyeId] = counts.TryGetValue(l.dyeId, out var n) ? n + 1 : 1;
            foreach (var kv in counts)
            {
                if (sb.Length > 0) sb.Append("、");
                sb.Append(Dye(c, kv.Key)).Append(kv.Value == 1 ? "一层" : kv.Value + "层");
            }
            return sb.ToString();
        }

        public static string Meters(double m) => (System.Math.Abs(m - System.Math.Round(m)) < 1e-6 ? ((int)System.Math.Round(m)).ToString() : m.ToString("0.#")) + "米";

        public static Color BoltColor(ConfigSnapshot c, Bolt b) => ClothLook.DyedColor(b.dyeLayers, c.balance.dye);

        public static Color DyeColor(ConfigSnapshot c, string dyeId)
        {
            var dc = c.balance.dye.ColorOf(dyeId);
            return dc == null ? Color.gray : new Color((float)dc.r, (float)dc.g, (float)dc.b);
        }

        /// <summary>来源句（docs/12 §2）：「精良 · 春茧平纹绢，靛蓝一套」的句式。</summary>
        public static string Source(SaveRoot s, ConfigSnapshot c, Garment g)
        {
            var outer = g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer");
            var b = outer != null ? Play.Find.Bolt(s, outer.boltId) : null;
            if (b == null) return Tier(g.tier) + " · " + g.name;
            string origin = b.id == NewGameFactory.OpeningBoltId ? "开局" : Season(s.season) + "茧";
            return Tier(g.tier) + " · " + origin + "平纹" + Variety(c, b.variety) + "，" + Layers(c, b) + "，汉风" + g.name;
        }
    }

    /// <summary>右侧实物侧架。</summary>
    public static class RackView
    {
        public enum Kind { Bolt, Yarn, Dye, Piece, Garment, Cocoon }

        /// <summary>侧架上干料与鲜料分开：鲜料的 id 加后缀。</summary>
        public const string FreshSuffix = "@fresh";
        public static string DyeKey(string dyeId, bool fresh) => fresh ? dyeId + FreshSuffix : dyeId;
        public static string DyeIdOf(string key, out bool fresh)
        {
            fresh = key != null && key.EndsWith(FreshSuffix);
            return fresh ? key.Substring(0, key.Length - FreshSuffix.Length) : key;
        }

        public static List<RackItem> Build(SaveRoot s, ConfigSnapshot c, System.Func<Kind, bool> show,
                                           string selectedId, System.Action<Kind, string> onClick, System.Func<Kind, string, bool> clickable = null)
        {
            var list = new List<RackItem>();
            void Add(Kind k, string id, string label, string sub, Color color)
            {
                if (show != null && !show(k)) return;
                bool can = onClick != null && (clickable == null || clickable(k, id));
                list.Add(new RackItem
                {
                    id = id, kind = k.ToString(), label = label, sub = sub, color = color, selected = id == selectedId,
                    interactable = can, onClick = can ? () => onClick(k, id) : (System.Action)null,
                });
            }
            foreach (var g in s.garments)
            {
                if (s.exhibit.slots.Exists(x => x.itemId == g.id)) continue; // 入柜后从侧架移走
                var outer = g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer");
                var b = outer != null ? Play.Find.Bolt(s, outer.boltId) : null;
                Add(Kind.Garment, g.id, g.name, Names.Tier(g.tier), b != null ? Names.BoltColor(c, b) : Color.white);
            }
            foreach (var p in s.pieces)
            {
                var b = Play.Find.Bolt(s, p.boltId);
                Add(Kind.Piece, p.id, Names.Slot(p.slot) + "衣片", p.sewScore.HasValue ? "已裁 · 已缝" : "已裁 · 未缝", b != null ? Names.BoltColor(c, b) : Color.white);
            }
            foreach (var b in s.bolts)
            {
                if (s.exhibit.slots.Exists(x => x.itemId == b.id)) continue;
                Add(Kind.Bolt, b.id, Names.Bolt(c, b), Names.Layers(c, b) + " · " + Names.Meters(b.length), Names.BoltColor(c, b));
            }
            foreach (var y in s.yarns)
                Add(Kind.Yarn, y.id, (y.fiber == "jiaCan" ? "家蚕" : y.fiber) + Names.Fineness(y.fineness) + "丝" + (y.id == NewGameFactory.OpeningYarnId ? " B" : ""), Names.Meters(y.length) + " · 尚未织", UiTheme.GauzeWhite);
            foreach (var cb in s.cocoons)
                Add(Kind.Cocoon, cb.id, "茧篮", "可缫 " + cb.batch + " 束", new Color(0.95f, 0.93f, 0.86f));
            foreach (var d in s.dyes)
                Add(Kind.Dye, DyeKey(d.dyeId, d.state == Play.Find.Fresh), (d.state == Play.Find.Fresh ? "鲜" : "干") + Names.Dye(c, d.dyeId), d.count + "份", Names.DyeColor(c, d.dyeId));
            return list;
        }
    }
}
