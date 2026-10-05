using System;
using System.Collections.Generic;

// docs/04 §2–§11 的常量，按小节分组。只有结构，没有数值。
// 每个字段的注释写明在 docs/04 的出处，导入器按出处读取。
namespace HuaShang.Rules.Config
{
    [Serializable]
    public class HourCost
    {
        /// <summary>feed / reel / weavePlain / weaveTwillSatin / weaveJacquard / dyeBath / dyeResist / cutPart / sewPart</summary>
        public string key;
        public string label;
        public int hours;
    }

    /// <summary>docs/04 §2</summary>
    [Serializable]
    public class DayData
    {
        public int hoursPerDay;
        /// <summary>第 1 个工坊日（日序 0）的教学日工时。</summary>
        public int teachingDayHours;

        /// <summary>某一日序的工时上限。</summary>
        public int HoursLimit(int dayIndex) => dayIndex == 0 ? teachingDayHours : hoursPerDay;
        public List<HourCost> hours = new List<HourCost>();
        /// <summary>「取消工序消耗一半工时，向上取整，至少 1」中的「至少」。</summary>
        public int cancelMinHours;
        public double boltLength;
        /// <summary>1 米纱织多少米布。</summary>
        public double clothPerYarnMeter;
        /// <summary>缫丝一束得纱米数。</summary>
        public double reelYieldMeters;
        public double widthDefault, widthMin, widthMax;

        public int HoursOf(string key)
        {
            foreach (var h in hours)
                if (h.key == key) return h.hours;
            throw new KeyNotFoundException("docs/04 §2 工时表中没有 " + key);
        }
    }

    /// <summary>docs/04 §3（首发只读春）</summary>
    [Serializable]
    public class SeasonData
    {
        public int springBase;
        public int feedGoodUniformity;
        public int overfeedPenalty;
        public int crowdingShapePenalty;
        public int summerHeatPerDay;
        public int seedPerTray;
        public int trayBatch;
        public int reelBeats;
        /// <summary>docs/04 §3 季节轮转：从几级开始、每季几个工坊日。</summary>
        public int rotationFromLevel;
        public int daysPerSeason;
        /// <summary>春夏秋冬四行，顺序即轮转顺序。</summary>
        public List<SeasonRow> rows = new List<SeasonRow>();

        public SeasonRow Row(string id) => rows.Find(r => r.id == id);
    }

    [Serializable]
    public class SeasonRow
    {
        public string id;
        public string label;
        /// <summary>蚕种品质基数；不结茧的季为 −1。</summary>
        public int cocoonBase;
        /// <summary>控温失败每天的热损（负数）。</summary>
        public int heatPerDay;
        /// <summary>当季新鲜染料 id。</summary>
        public List<string> freshDyes = new List<string>();
        public bool CanHatch => cocoonBase >= 0;
    }

    /// <summary>docs/04 §4 Q 的七项权重</summary>
    [Serializable]
    public class QualityWeights
    {
        public double material, yarn, weave, dye, finish, cut, sew;
    }

    [Serializable]
    public class TierBand
    {
        /// <summary>common / fine / legendary（普通 / 精良 / 传世）</summary>
        public string tier;
        public string label;
        public int qMin, qMax;
        /// <summary>t = tBase + (Q − qOffset) / denominator × span</summary>
        public double tBase;
        public double qOffset;
        public double denominator;
        public double span;
    }

    [Serializable]
    public class DynastyOffset
    {
        /// <summary>han / tang / song / ming</summary>
        public string dynasty;
        public string label;
        /// <summary>首发是否启用（docs/04 §4：首发三种品种只用汉偏移）。</summary>
        public bool launch;
        /// <summary>该行能否按「×数字」读出。唐行原文带条件，读不出时为 false，不得启用。</summary>
        public bool parsed;
        public double density, bend, wind, gloss;
    }

    [Serializable]
    public class PresentationClamp
    {
        /// <summary>history / standard / enhanced</summary>
        public string mode;
        public string label;
        public bool hasTMax;
        public double tMax;
        public bool hasTMin;
        public double tMin;
        public double glossFactor = 1;
        public double gauzeWindFactor = 1;
        public int syncCapPercent;
    }

    /// <summary>docs/04 §4</summary>
    [Serializable]
    public class QualityData
    {
        public QualityWeights weights = new QualityWeights();
        public double undyedDyeScore;
        public double unfinishedFinishScore;
        public double layerOuter, layerMiddle, layerInner;
        public List<TierBand> bands = new List<TierBand>();
        public List<DynastyOffset> dynasties = new List<DynastyOffset>();
        public List<PresentationClamp> presentation = new List<PresentationClamp>();
    }

    /// <summary>docs/04 §5 扣分与修正表</summary>
    [Serializable]
    public class PenaltyData
    {
        public int reelBreak;
        public double chaosRatioThreshold;
        public int chaosSegmentCap;
        public int wrongPatternMotifScore;
        public int orientation90;
        public int seamFront;
        public int legendaryInnerWaste;
        public int sewOrder;
        public int formSkipped;
        public int sewLowThreshold;
        public double edgeGlossPerPoint;
        public int edgeNoLegendaryOuter;
        public int unpickEdgeDamage;
    }

    /// <summary>docs/04 §5 织造分</summary>
    [Serializable]
    public class WeaveData
    {
        public double rhythm, densityStable, motif;
        public double noMotifScore;
        public int steadyWindowMs, offWindowMs;
        /// <summary>节拍分：稳 / 偏 / 乱。</summary>
        public double beatSteady, beatOff, beatChaos;
        /// <summary>节拍间隔（秒）。</summary>
        public double beatInterval;
        public int beatsPerSegment, segments;
        public int sewNeedlesPerPart;
        public double cutStartScore;
        /// <summary>织造分低于此线为残布。</summary>
        public double defectLine;
        /// <summary>docs/04 §8：粗丝织纱的织造分上限。</summary>
        public double coarseGauzeCap;
        /// <summary>docs/04 §8：传世素纱的接头上限。</summary>
        public int legendaryGauzeMaxJoints;
    }

    [Serializable]
    public class DyeColor
    {
        /// <summary>染料 id；未染底色为 "base"。</summary>
        public string dyeId;
        public string label;
        /// <summary>sRGB，0–1。</summary>
        public double r, g, b;
    }

    [Serializable]
    public class NamedValue
    {
        public string key;
        public string label;
        public double value;
    }

    [Serializable]
    public class TreatmentMod
    {
        /// <summary>plantLayer / strongExtra / fromSecondLayer / vitriol / calender / goldMask</summary>
        public string key;
        public string label;
        public double damping;
        public double bend;
        /// <summary>弯曲按倍数修正时的倍数（金线掩膜），否则为 1。</summary>
        public double bendFactor = 1;
        public double gloss;
    }

    /// <summary>docs/04 §5 染色</summary>
    [Serializable]
    public class DyeData
    {
        /// <summary>light / medium / strong（淡 / 中 / 浓）</summary>
        public List<NamedValue> concentration = new List<NamedValue>();
        public double strengthCap;
        public double scoreTemp, scoreTime, scoreStir;
        public double colorMix;
        public int maxLayers;
        public int overflowPenalty;
        public double opacityPerLayer;
        public List<TreatmentMod> mods = new List<TreatmentMod>();
        /// <summary>新鲜染料的浓度系数乘数（docs/04 §5）。</summary>
        public double freshConcentrationFactor;
        /// <summary>每浸染一匹耗干染料份数。</summary>
        public int costPerBolt;
        /// <summary>水温：最佳档名与吻合度。</summary>
        public string bestTemp;
        public double tempMatchBest, tempMatchOther;
        /// <summary>起布提示带。</summary>
        public double liftBandWidth;
        public List<NamedValue> liftCenter = new List<NamedValue>();
        public double liftPenaltyPerSecond, liftMatchMin;
        /// <summary>搅拌：偏拍折算系数。</summary>
        public double stirOffWeight;
        public double unevenAllowedMax, livelyMin;
        public List<DyeColor> colors = new List<DyeColor>();

        public double LiftCenterOf(string key)
        {
            foreach (var c in liftCenter)
                if (c.key == key) return c.value;
            throw new KeyNotFoundException("docs/04 §5 起布提示带中没有 " + key);
        }

        public DyeColor ColorOf(string dyeId)
        {
            foreach (var c in colors)
                if (c.dyeId == dyeId) return c;
            return null;
        }

        public double ConcentrationOf(string key)
        {
            foreach (var c in concentration)
                if (c.key == key) return c.value;
            throw new KeyNotFoundException("docs/04 §5 浓度系数中没有 " + key);
        }

        public TreatmentMod ModOf(string key)
        {
            foreach (var m in mods)
                if (m.key == key) return m;
            throw new KeyNotFoundException("docs/04 §5 染色修正表中没有 " + key);
        }
    }

    [Serializable]
    public class AffectionEvent
    {
        /// <summary>fitHigh / fitMid / fitLow / themeQuest / legendaryReplay</summary>
        public string key;
        public string label;
        public int value;
    }

    [Serializable]
    public class TierScore
    {
        public int tier;
        public int score;
    }

    [Serializable]
    public class TraitRow
    {
        public string name;
        public string condition;
        public string effect;
        /// <summary>条件里「≥ n」的全部 n，按出现顺序。</summary>
        public List<int> thresholds = new List<int>();
    }

    /// <summary>docs/04 §6</summary>
    [Serializable]
    public class AffectionData
    {
        public List<AffectionEvent> events = new List<AffectionEvent>();
        public int fitHighMin, fitMidMin;
        public List<int> tierThresholds = new List<int>();
        public int fitVariety, fitDynasty, fitTrait, fitNote, fitBase;
        public double heatQ, heatFit, heatStage;
        public List<TierScore> stageScores = new List<TierScore>();
        public int maxTraits;
        public List<TraitRow> traits = new List<TraitRow>();
    }

    [Serializable]
    public class LevelRow
    {
        public int level;
        public int xp;
        public bool launch;
    }

    /// <summary>docs/04 §7</summary>
    [Serializable]
    public class EconomyData
    {
        public int startCoin;
        public int exhibitPointsPer, exhibitCoinsPer, exhibitCoinCap;
        public int sellCommonPerMeter;
        public int dryDyePrice, threadPrice, goldThreadPrice, seedPrice, freshDyePrice;
        public double xpPerHour;
        public double xpCommon, xpFine, xpLegendary;
        public double xpDefectFactor;
        public List<LevelRow> levels = new List<LevelRow>();
    }

    [Serializable]
    public class ColorTemp
    {
        public string label;
        public int kelvin;
    }

    /// <summary>docs/04 §9</summary>
    [Serializable]
    public class WindLightData
    {
        public double baseWindMin, baseWindMax;
        public double tier30WindMax;
        public double tier70PeakMin, tier70PeakMax;
        public double tier70EndMax;
        public int maxConcurrentWinds;
        public int rimPercentInit;
        public List<ColorTemp> colorTemps = new List<ColorTemp>();
        public List<TimelineLength> timelines = new List<TimelineLength>();

        public TimelineLength TimelineOf(string characterId, int tier)
        {
            foreach (var t in timelines)
                if (t.characterId == characterId && t.tier == tier) return t;
            return null;
        }
    }

    /// <summary>docs/04 §9 首发时间轴的段落长度（秒）。</summary>
    [Serializable]
    public class TimelineLength
    {
        public string characterId;
        public int tier;
        public double setup, develop, climax, coda;
        public double Total => setup + develop + climax + coda;
    }

    /// <summary>docs/04 §10</summary>
    [Serializable]
    public class ExhibitData
    {
        public double quality, theme, label, image;
        public int minItems;
        /// <summary>docs/09 §3：首发柜内展位数。</summary>
        public int launchSlots;
    }

    /// <summary>docs/10 §3 日常委托。</summary>
    [Serializable]
    public class QuestRules
    {
        public int dailyFromLevel;
        public int dailyCount;
        public int maxHeld;
        public int days;
    }

    [Serializable]
    public class BalanceData
    {
        public DayData day = new DayData();
        public SeasonData season = new SeasonData();
        public QualityData quality = new QualityData();
        public PenaltyData penalties = new PenaltyData();
        public WeaveData weave = new WeaveData();
        public DyeData dye = new DyeData();
        public AffectionData affection = new AffectionData();
        public EconomyData economy = new EconomyData();
        public WindLightData windLight = new WindLightData();
        public ExhibitData exhibit = new ExhibitData();
        public QuestRules questRules = new QuestRules();
        /// <summary>docs/04 §11：参数写进布料配置时保留的小数位。</summary>
        public int clothDecimals;
    }
}
