using System;
using System.Collections.Generic;

namespace HuaShang.Rules.Config
{
    /// <summary>
    /// 全部配置表的纯数据快照。纯函数与测试只读它；运行时由 ScriptableObject 组装，
    /// 编辑器导入时由 docs 直接读出。
    /// </summary>
    [Serializable]
    public class ConfigSnapshot
    {
        public List<VarietyRow> varieties = new List<VarietyRow>();
        public List<StretchGroupRow> stretchGroups = new List<StretchGroupRow>();
        public List<DyeRow> dyes = new List<DyeRow>();
        public List<PatternRow> patterns = new List<PatternRow>();
        public List<CharacterRow> characters = new List<CharacterRow>();
        public List<StageRow> stages = new List<StageRow>();
        public List<QuestRow> quests = new List<QuestRow>();
        public List<ActivityRow> activities = new List<ActivityRow>();
        public List<UnlockRow> unlocks = new List<UnlockRow>();
        public BalanceData balance = new BalanceData();
        public OpeningData opening = new OpeningData();
        public ClothFixed clothFixed = new ClothFixed();
        /// <summary>导入时的提示（暂定 id、无法对应的原文等），不影响加载。</summary>
        public List<string> notes = new List<string>();

        public VarietyRow Variety(string id) => Find(varieties, v => v.id == id, "品种", id);
        public StretchGroupRow StretchGroup(string id) => Find(stretchGroups, g => g.id == id, "分组", id);
        public DyeRow Dye(string id) => Find(dyes, d => d.id == id, "染料", id);
        public CharacterRow Character(string id) => Find(characters, c => c.id == id, "角色", id);

        public DynastyOffset Dynasty(string dynasty)
        {
            return Find(balance.quality.dynasties, d => d.dynasty == dynasty, "朝代偏移", dynasty);
        }

        static T Find<T>(List<T> list, Predicate<T> match, string what, string id)
        {
            var found = list.Find(match);
            if (found == null) throw new KeyNotFoundException("配置中没有" + what + " " + id);
            return found;
        }
    }
}
