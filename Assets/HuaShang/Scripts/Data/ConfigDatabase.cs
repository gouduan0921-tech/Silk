using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Data
{
    /// <summary>
    /// 汇总全部配置表。运行时只读：玩法模块拿 ToSnapshot() 的结果调用纯函数。
    /// </summary>
    [CreateAssetMenu(menuName = "HuaShang/配置总表", fileName = "ConfigDatabase")]
    public class ConfigDatabase : ScriptableObject
    {
        public VarietyTable varieties;
        public StretchGroupTable stretchGroups;
        public DyeTable dyes;
        public PatternTable patterns;
        public CharacterTable characters;
        public StageTable stages;
        public QuestTable quests;
        public ActivityTable activities;
        public UnlockTable unlocks;
        public BalanceConstants balance;
        public OpeningSave opening;
        public ClothFixedSettings clothFixed;

        public ConfigSnapshot ToSnapshot()
        {
            var missing = new List<string>();
            if (varieties == null) missing.Add("varieties");
            if (stretchGroups == null) missing.Add("stretchGroups");
            if (dyes == null) missing.Add("dyes");
            if (patterns == null) missing.Add("patterns");
            if (characters == null) missing.Add("characters");
            if (stages == null) missing.Add("stages");
            if (quests == null) missing.Add("quests");
            if (activities == null) missing.Add("activities");
            if (unlocks == null) missing.Add("unlocks");
            if (balance == null) missing.Add("balance");
            if (opening == null) missing.Add("opening");
            if (clothFixed == null) missing.Add("clothFixed");
            if (missing.Count > 0)
                throw new System.InvalidOperationException("ConfigDatabase 缺少：" + string.Join("、", missing) + "。请先运行「HuaShang/从 docs 导入配置表」。");

            return new ConfigSnapshot
            {
                varieties = varieties.rows,
                stretchGroups = stretchGroups.rows,
                dyes = dyes.rows,
                patterns = patterns.rows,
                characters = characters.rows,
                stages = stages.rows,
                quests = quests.rows,
                activities = activities.rows,
                unlocks = unlocks.rows,
                balance = balance.balance,
                opening = opening.opening,
                clothFixed = clothFixed.values,
            };
        }

        /// <summary>docs/05 §9 加载校验。失败时报错，不进入游戏。</summary>
        public ConfigSnapshot LoadValidated()
        {
            var snapshot = ToSnapshot();
            var errors = ConfigValidator.Validate(snapshot);
            if (errors.Count > 0)
            {
                foreach (var e in errors) Debug.LogError("[HuaShang 配置] " + e);
                throw new System.InvalidOperationException("配置校验未通过，共 " + errors.Count + " 条，见控制台。");
            }
            return snapshot;
        }
    }
}
