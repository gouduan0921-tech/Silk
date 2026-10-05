using System.Collections.Generic;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>等级只打开 docs/10 §1 标为首发的内容；等级不提高风力，好感不给属性点（docs/10 §4）。</summary>
    public static class Unlocks
    {
        static IEnumerable<UnlockRow> Open(SaveRoot s, ConfigSnapshot c)
        {
            foreach (var u in c.unlocks)
                if (u.launch && u.level <= s.level) yield return u;
        }

        public static bool VarietyOpen(SaveRoot s, ConfigSnapshot c, string id)
        {
            foreach (var u in Open(s, c)) if (u.varieties.Contains(id)) return true;
            // 苎麻只作衬里：首发开局即有（docs/00 §6），不在等级表里
            var v = c.varieties.Find(x => x.id == id);
            return v != null && v.launch && v.liningOnly;
        }

        public static bool DyeOpen(SaveRoot s, ConfigSnapshot c, string id)
        {
            foreach (var u in Open(s, c)) if (u.dyes.Contains(id)) return true;
            return false;
        }

        public static bool PatternOpen(SaveRoot s, ConfigSnapshot c, string id)
        {
            foreach (var u in Open(s, c)) if (u.patterns.Contains(id)) return true;
            return false;
        }

        /// <summary>角色是否由等级打开，以及等级表写明的档位（空表示基础演出）。</summary>
        public static bool CharacterOpen(SaveRoot s, ConfigSnapshot c, string id, out List<int> tiers)
        {
            tiers = null;
            var row = c.characters.Find(x => x.id == id);
            if (row == null || !row.enabled) return false;
            foreach (var u in Open(s, c))
                foreach (var ch in u.characters)
                    if (ch.characterId == id) { tiers = ch.tiers; return true; }
            return false;
        }

        /// <summary>等级表里不对应表格行的条目，例如「提花花本」「宋风」。</summary>
        public static bool OtherOpen(SaveRoot s, ConfigSnapshot c, string token)
        {
            foreach (var u in Open(s, c)) if (u.other.Contains(token)) return true;
            return false;
        }

        public static List<string> OpenPatterns(SaveRoot s, ConfigSnapshot c)
        {
            var list = new List<string>();
            foreach (var p in c.patterns) if (p.launch && PatternOpen(s, c, p.id)) list.Add(p.id);
            return list;
        }

        public static List<string> OpenVarieties(SaveRoot s, ConfigSnapshot c)
        {
            var list = new List<string>();
            foreach (var v in c.varieties) if (v.launch && !v.liningOnly && VarietyOpen(s, c, v.id)) list.Add(v.id);
            return list;
        }

        public static List<string> OpenDyes(SaveRoot s, ConfigSnapshot c)
        {
            var list = new List<string>();
            foreach (var d in c.dyes) if (d.launch && DyeOpen(s, c, d.id)) list.Add(d.id);
            return list;
        }
    }
}
