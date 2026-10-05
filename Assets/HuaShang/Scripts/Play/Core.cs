using System;
using System.Collections.Generic;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

// 玩法命令：普通 C# 纯函数，只改传入的存档对象（构架文档 §2）。
// 界面只调用这些命令，不自己算分、不自己扣库存。
namespace HuaShang.Play
{
    /// <summary>命令结果。失败时存档不变。</summary>
    public class Result
    {
        public bool ok;
        public string error;
        public readonly List<string> notes = new List<string>();
        public string createdId;

        public static Result Fail(string error) => new Result { ok = false, error = error };
        public static Result Ok(string createdId = null) => new Result { ok = true, createdId = createdId };
    }

    /// <summary>节拍判定（docs/04 §5 节奏窗口）。</summary>
    public enum Beat
    {
        Steady = 0,
        Off = 1,
        Chaos = 2,
    }

    public static class Beats
    {
        /// <summary>按偏差毫秒判定稳、偏、乱。</summary>
        public static Beat Judge(double offsetMs, WeaveData w)
        {
            double a = Math.Abs(offsetMs);
            if (a <= w.steadyWindowMs) return Beat.Steady;
            if (a <= w.offWindowMs) return Beat.Off;
            return Beat.Chaos;
        }

        public static double ScoreOf(Beat b, WeaveData w)
        {
            switch (b)
            {
                case Beat.Steady: return w.beatSteady;
                case Beat.Off: return w.beatOff;
                default: return w.beatChaos;
            }
        }

        public static double Average(IList<Beat> beats, WeaveData w)
        {
            if (beats == null || beats.Count == 0) return 0;
            double sum = 0;
            foreach (var b in beats) sum += ScoreOf(b, w);
            return sum / beats.Count;
        }

        public static double Ratio(IList<Beat> beats, Beat kind)
        {
            if (beats == null || beats.Count == 0) return 0;
            int n = 0;
            foreach (var b in beats) if (b == kind) n++;
            return n / (double)beats.Count;
        }
    }

    public static class Ids
    {
        /// <summary>确定性的新 id：前缀 + 日序 + 序号，跳过已存在的。</summary>
        public static string Next(SaveRoot s, string prefix)
        {
            for (int i = 1; ; i++)
            {
                string id = prefix + "_" + s.dayIndex + "_" + i;
                if (!Exists(s, id)) return id;
            }
        }

        static bool Exists(SaveRoot s, string id)
        {
            return s.bolts.Exists(x => x.id == id) || s.yarns.Exists(x => x.id == id) || s.pieces.Exists(x => x.id == id)
                || s.garments.Exists(x => x.id == id) || s.cocoons.Exists(x => x.id == id)
                || s.performances.Exists(x => x.id == id) || s.quests.Exists(x => x.id == id);
        }
    }

    /// <summary>工时与经验（docs/04 §2、§7）。</summary>
    public static class Progress
    {
        public static int HoursLeft(SaveRoot s, ConfigSnapshot c) => c.balance.day.HoursLimit(s.dayIndex) - s.hoursUsed;

        /// <summary>如实检查工时；不足就失败，不做任何豁免（构架文档 G1，已由 docs/04 §2 教学日解决）。</summary>
        public static bool CanSpend(SaveRoot s, ConfigSnapshot c, int hours) => s.hoursUsed + hours <= c.balance.day.HoursLimit(s.dayIndex);

        public static void Spend(SaveRoot s, int hours) => s.hoursUsed += hours;

        /// <summary>工序完成按普通系数给经验；残次给一半（docs/04 §7）。</summary>
        public static int AwardProcessXp(SaveRoot s, ConfigSnapshot c, int hours, bool defect)
        {
            var e = c.balance.economy;
            double xp = hours * e.xpPerHour * e.xpCommon * (defect ? e.xpDefectFactor : 1);
            int gain = (int)Math.Round(xp, MidpointRounding.AwayFromZero);
            AddXp(s, c, gain);
            return gain;
        }

        public static double TierFactor(string tier, EconomyData e)
        {
            switch (tier)
            {
                case QualityCalc.Legendary: return e.xpLegendary;
                case QualityCalc.Fine: return e.xpFine;
                default: return e.xpCommon;
            }
        }

        /// <summary>加经验并按 docs/04 §7 升级；只升到首发开放的等级。</summary>
        public static void AddXp(SaveRoot s, ConfigSnapshot c, int gain)
        {
            s.xp += gain;
            foreach (var lv in c.balance.economy.levels)
                if (lv.launch && s.xp >= lv.xp && lv.level > s.level) s.level = lv.level;
        }
    }

    public static class Find
    {
        public static Bolt Bolt(SaveRoot s, string id) => s.bolts.Find(x => x.id == id);
        public static Yarn Yarn(SaveRoot s, string id) => s.yarns.Find(x => x.id == id);
        public static Piece Piece(SaveRoot s, string id) => s.pieces.Find(x => x.id == id);
        public static Garment Garment(SaveRoot s, string id) => s.garments.Find(x => x.id == id);
        public const string Dry = "dry";
        public const string Fresh = "fresh";

        public static DyeStock Dye(SaveRoot s, string dyeId) => s.dyes.Find(x => x.dyeId == dyeId && x.state == Dry);

        public static DyeStock Dye(SaveRoot s, string dyeId, bool fresh) => s.dyes.Find(x => x.dyeId == dyeId && x.state == (fresh ? Fresh : Dry));
        public static QuestState Quest(SaveRoot s, string id) => s.quests.Find(x => x.id == id);

        public static CharacterState Character(SaveRoot s, string id)
        {
            var ch = s.characters.Find(x => x.id == id);
            if (ch == null)
            {
                ch = new CharacterState { id = id };
                s.characters.Add(ch);
            }
            return ch;
        }
    }
}
