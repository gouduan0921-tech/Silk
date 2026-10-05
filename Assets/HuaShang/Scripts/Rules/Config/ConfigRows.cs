using System;
using System.Collections.Generic;

// 配置表的行结构，字段对应 docs/05。
// 本文件只定义结构，不含任何数值。数值由 HuaShang.DocImport 从 docs/04 等文件读入，
// 再由编辑器导入器写进 Assets/HuaShang/Data 下的 ScriptableObject。
// 关闭的行保留，launch 或 enabled 为 false。
namespace HuaShang.Rules.Config
{
    /// <summary>docs/05 §2 品种。区间来自 docs/04 §8。</summary>
    [Serializable]
    public class VarietyRow
    {
        public string id;
        public string name;
        /// <summary>gauze / plain / brocade / bast</summary>
        public string group;
        public bool launch;
        /// <summary>docs/05 只给出首发四个 id（suSha、juan、chou、zhuMa），其余为暂定拼写，待 docs/05 确认。</summary>
        public bool idProvisional;
        /// <summary>docs/04 §8「是，仅衬里」。docs/05 §2 尚无此字段，待补。</summary>
        public bool liningOnly;
        public double densityMin, densityMax;   // gravity
        public double bendMin, bendMax;         // angleRestorationConstraint.stiffness
        public double windMin, windMax;         // wind.influence
        public double glossMin, glossMax;       // 1–10 观感标尺，进材质
        public double opacityMin, opacityMax;   // 1–10 观感标尺，进材质
        /// <summary>指向 docs/04 §8 分组表，与 group 同名。</summary>
        public string stretchGroup;
    }

    /// <summary>docs/04 §8 距离刚度与阻尼分组表。</summary>
    [Serializable]
    public class StretchGroupRow
    {
        public string id;
        public string name;
        public double distanceMin, distanceMax; // distanceConstraint.stiffness
        public double dampingMin, dampingMax;   // damping
    }

    /// <summary>docs/05 §3 染料。</summary>
    [Serializable]
    public class DyeRow
    {
        public string id;
        public string name;
        /// <summary>color / mordant</summary>
        public string role;
        public bool launch;
        /// <summary>spring / summer / autumn / any</summary>
        public string bestSeason;
    }

    [Serializable]
    public class SlotLength
    {
        public string slot;
        public double length;
    }

    /// <summary>docs/05 §4 形制。用量来自 docs/04 §2。</summary>
    [Serializable]
    public class PatternRow
    {
        public string id;
        public string name;
        public List<string> parts = new List<string>();
        public List<string> optionalParts = new List<string>();
        public List<SlotLength> partLengths = new List<SlotLength>();
        /// <summary>han / tang / song / ming</summary>
        public string dynasty;
        public bool launch;
    }

    /// <summary>docs/05 §5 角色，内容见 docs/16。</summary>
    [Serializable]
    public class CharacterRow
    {
        public string id;
        public string name;
        /// <summary>airy / cold / lively / rich / strong</summary>
        public string template;
        public List<string> preferVariety = new List<string>();
        public string preferDynasty;
        public int launchTierMax;
        public bool enabled;
        /// <summary>篇章特效（docs/11 §3）：water / moon；空为无。</summary>
        public string stageEffect = "";
        /// <summary>偏好列里无法对上品种 id 的原文，供策划核对。</summary>
        public string note;
        /// <summary>docs/16 §2：某偏好品种首发不可选时，由另一品种代为计品种命中。</summary>
        public List<VarietySubstitute> substitutes = new List<VarietySubstitute>();
    }

    [Serializable]
    public class VarietySubstitute
    {
        public string whenMissing;
        public string countsAs;
    }

    /// <summary>docs/05 §6 舞台。</summary>
    [Serializable]
    public class StageRow
    {
        public string id;
        public bool launch;
    }

    /// <summary>docs/05 §7 委托。P0 只登记教学委托的顺序与原文，条件在 P3 结构化。</summary>
    [Serializable]
    public class QuestRow
    {
        public string id;
        public bool tutorial;
        public int order;
        public string description;
        /// <summary>-1 表示未设。</summary>
        public int days = -1;
        /// <summary>-1 表示无。</summary>
        public int rewardAffection = -1;
    }

    /// <summary>docs/05 §8 活动。默认关闭，只允许白名单两项。</summary>
    [Serializable]
    public class ActivityRow
    {
        public string id;
        public bool enabled;
        public string dyeId;
        public double dyeSeasonStrength = 1;
        public int exhibitCoinCap = -1;
    }

    [Serializable]
    public class CharacterTierUnlock
    {
        public string characterId;
        /// <summary>原文写明的档位；未写则为空，表示该角色的基础演出。</summary>
        public List<int> tiers = new List<int>();
    }

    /// <summary>docs/10 §1 等级解锁，经验门槛来自 docs/04 §7。</summary>
    [Serializable]
    public class UnlockRow
    {
        public int level;
        public int xpRequired;
        public bool launch;
        public List<string> varieties = new List<string>();
        public List<string> dyes = new List<string>();
        public List<string> patterns = new List<string>();
        public List<CharacterTierUnlock> characters = new List<CharacterTierUnlock>();
        /// <summary>尚未对应到表 id 的条目原文（如「平纹」「委托刷新」）。</summary>
        public List<string> other = new List<string>();
        public string raw;
    }

    /// <summary>docs/03 §2 不按品种分的固定项。</summary>
    [Serializable]
    public class ClothFixed
    {
        public double gravityFalloff;
        public double triangleBendingStiffness;
        public double radius;
        /// <summary>docs/03 §4：α = 1 − 透明度² × 此值。</summary>
        public double alphaPerOpacity;
        /// <summary>docs/03 §4：平滑度 = 光泽 × 此值。</summary>
        public double smoothnessPerGloss;
        /// <summary>docs/03 §3：走近展柜多少米内，当前一件升到高。</summary>
        public double museumNearMeters;
    }

    [Serializable]
    public class DyeCount
    {
        public string dyeId;
        public int count;
        /// <summary>dry / fresh</summary>
        public string state;
    }

    [Serializable]
    public class OpeningBolt
    {
        public string variety;
        public string fineness;
        public int materialScore;
        public int yarnScore;
        public int weaveScore;
        public bool dyed;
        public double length;
        public double width;
    }

    [Serializable]
    public class OpeningYarn
    {
        public string fiber;
        public string fineness;
        public int materialScore;
        public int processScore;
        public int joints;
        public double length;
    }

    /// <summary>docs/04 §1 开局。</summary>
    [Serializable]
    public class OpeningData
    {
        public int silkCoin;
        public int xp;
        public int level;
        public int xiShiAffection;
        public string season;
        public bool trayEmpty;
        public OpeningBolt bolt = new OpeningBolt();
        public OpeningYarn yarn = new OpeningYarn();
        public List<DyeCount> dyes = new List<DyeCount>();
    }
}
