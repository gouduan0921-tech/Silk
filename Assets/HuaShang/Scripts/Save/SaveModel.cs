using System.Collections.Generic;
using HuaShang.Rules.Calc;

// docs/21 0.3 的 schema 1。字段名与手册一致；可空的字段用可空类型，存成 JSON 的 null。
// 只有存档模块写盘；各字段由谁改见构架文档 §2.1。
namespace HuaShang.Save
{
    public class SaveRoot
    {
        public int schema;
        public int dayIndex;
        public string season;
        /// <summary>季节轮转开始的日序（docs/04 §3）；未开始为空。</summary>
        public int? seasonStartDay;
        public int silkCoin;
        public int xp;
        public int level;
        /// <summary>当日已用工时，工坊日结束清零（docs/21 §2）。</summary>
        public int hoursUsed;
        public List<Yarn> yarns = new List<Yarn>();
        public List<DyeStock> dyes = new List<DyeStock>();
        public Tray tray = new Tray();
        public List<CocoonBasket> cocoons = new List<CocoonBasket>();
        public List<Piece> pieces = new List<Piece>();
        public List<Bolt> bolts = new List<Bolt>();
        public List<Garment> garments = new List<Garment>();
        public List<CharacterState> characters = new List<CharacterState>();
        public List<PerformanceRecord> performances = new List<PerformanceRecord>();
        public Exhibit exhibit = new Exhibit();
        public List<QuestState> quests = new List<QuestState>();
        /// <summary>history / standard / enhanced</summary>
        public string presentMode;
        /// <summary>档 id，随机种子的盐。</summary>
        public string rngSalt;
    }

    /// <summary>docs/21 §3</summary>
    public class Yarn
    {
        public string id;
        public string fiber;
        public string fineness;
        public int? processScore;
        /// <summary>开局丝 B 在 docs/04 §1 只给了过程分，原料分为空。</summary>
        public int? materialScore;
        /// <summary>开局丝 B 未给接头数，为空。</summary>
        public int? joints;
        public double length;
    }

    /// <summary>docs/21 §4</summary>
    public class Bolt
    {
        public string id;
        public string variety;
        public string dynastyStyle;
        /// <summary>开局布 A 没有对应的纱线，为空。</summary>
        public string yarnId;
        /// <summary>下机时从纱线拷入；开局布 A 取 docs/04 §1。</summary>
        public int? materialScore;
        public int? yarnScore;
        public double length;
        public double width;
        public int? weaveScore;
        public string patternId;
        public List<DyeLayer> dyeLayers = new List<DyeLayer>();
        public string finish;
        public int edgeDamage;
        /// <summary>挤层扣分累计（docs/21 §4）。</summary>
        public int dyePenalty;
        /// <summary>可以不存，读档时由解算模块重算（S2）。</summary>
        public ClothDescriptor cloth;
        /// <summary>若存在，读档不重算、不覆盖（S3）。</summary>
        public ClothDescriptor clothLocked;
    }

    /// <summary>docs/21 §5 的 scores：七项过程分，缺项为空。</summary>
    public class GarmentScores
    {
        public double? material, yarn, weave, dye, finish, cut, sew;
    }

    public class GarmentPart
    {
        public string slot;
        public string boltId;
        public double lengthUsed;
        public int? cutScore;
        public int? sewScore;
    }

    /// <summary>docs/21 §5</summary>
    public class Garment
    {
        public string id;
        public string name;
        public string pattern;
        public string dynastyStyle;
        public List<GarmentPart> parts = new List<GarmentPart>();
        public GarmentScores scores = new GarmentScores();
        /// <summary>只能由 docs/04 §4 的公式写入；算不出时为空，界面显示「待评定」。</summary>
        public string tier;
        public List<string> traits = new List<string>();
        public bool previewSeen;
        /// <summary>docs/21 §7：单件覆盖呈现模式，为空表示跟随根。</summary>
        public string presentMode;
    }

    /// <summary>docs/21 §8</summary>
    public class DyeStock
    {
        public string dyeId;
        public int count;
        /// <summary>dry / fresh</summary>
        public string state;
    }

    /// <summary>docs/21 §9。空箔时 stage 为 empty。</summary>
    public class Tray
    {
        /// <summary>empty / hatched / fed / tempered / mounted</summary>
        public string stage = TrayStage.Empty;
        public int? startDay;
        public int? lastStepDay;
        public int? shellScore;
        public int? uniformity;
        public int? shapeScore;
        public bool sick;
    }

    public static class TrayStage
    {
        public const string Empty = "empty";
        public const string Hatched = "hatched";
        public const string Fed = "fed";
        public const string Tempered = "tempered";
        public const string Mounted = "mounted";
    }

    /// <summary>docs/21 §9</summary>
    public class CocoonBasket
    {
        public string id;
        public int shellScore;
        public int uniformity;
        public int shapeScore;
        public int batch;
        public bool defect;
    }

    /// <summary>docs/21 §10：已裁、尚未收成成衣的衣片。</summary>
    public class Piece
    {
        public string id;
        public string slot;
        public string boltId;
        public double lengthUsed;
        /// <summary>narrow / standard / wide（docs/07 §3）。</summary>
        public string sizeClass = "standard";
        public int? cutScore;
        /// <summary>未缝为空。</summary>
        public int? sewScore;
    }

    public class CharacterState
    {
        public string id;
        public int affection;
        public List<int> unlockedTiers = new List<int>();
        /// <summary>主题委托的好感每角色一次（docs/04 §6）。</summary>
        public bool themeQuestRewarded;
    }

    public class PerformanceRecord
    {
        public string id;
        public string characterId;
        public int tier;
        public string garmentId;
        public string presentMode;
        public double? heat;
        public double? fit;
        public int dayIndex;
    }

    public class ExhibitLabel
    {
        /// <summary>docs/09 §3：不超过 40 字。</summary>
        public string title;
        public bool showMaterial;
        public bool showDyeLayers;
        public bool showDynasty;
    }

    public class ExhibitSlot
    {
        public int index;
        /// <summary>garment / bolt</summary>
        public string itemKind;
        public string itemId;
        public ExhibitLabel label = new ExhibitLabel();
    }

    public class Exhibit
    {
        public List<ExhibitSlot> slots = new List<ExhibitSlot>();
        /// <summary>yiSe（一色）/ canToSi（从蚕到丝），docs/09 §4。</summary>
        public string theme = "yiSe";
        public int? lastPaidDay;
    }

    public class QuestState
    {
        public string id;
        public bool done;
        public bool tutorial;
        /// <summary>日常委托：bolt / garment。</summary>
        public string needKind;
        public string needVariety;
        public string needDye;
        public string needMinTier;
        public string characterId;
        public int createdDay;
        public int expireDay;
        public bool expired;
        /// <summary>交付的物品 id：从仓库移到委托记录（docs/10 §3）。</summary>
        public string deliveredItemId;
    }
}
