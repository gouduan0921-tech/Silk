using System;
using System.Text;
using HuaShang.Rules.Config;

namespace HuaShang.Rules.Calc
{
    /// <summary>docs/02 §3 与 docs/04 §5 的层位资格。</summary>
    public static class LayerRules
    {
        /// <summary>边损达到门槛的布不能做传世外层（N6）。</summary>
        public static bool CanBeLegendaryOuter(int edgeDamage, PenaltyData p)
        {
            return edgeDamage < p.edgeNoLegendaryOuter;
        }
    }

    /// <summary>docs/04 §2 工时。</summary>
    public static class HoursCalc
    {
        /// <summary>取消工序消耗一半工时，向上取整，至少为 docs/04 §2 的下限（N12）。</summary>
        public static int CancelCost(int processHours, DayData day)
        {
            int half = (int)Math.Ceiling(processHours / 2.0);
            return Math.Max(day.cancelMinHours, half);
        }

        /// <summary>
        /// 当日剩余工时是否够做这道工序。只如实回答，不做教学豁免（构架文档 G1）。
        /// </summary>
        public static bool CanSpend(int hoursUsedToday, int cost, DayData day)
        {
            return hoursUsedToday + cost <= day.hoursPerDay;
        }
    }

    /// <summary>docs/19 §4：随机只用 hash(档 id, 日序)，读档不重掷。</summary>
    public static class StableHash
    {
        /// <summary>FNV-1a 64 位，输入为 UTF-8 的「档 id + '|' + 日序」。跨平台、跨版本稳定。</summary>
        public static ulong Of(string saveId, int dayIndex)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            ulong h = offset;
            byte[] bytes = Encoding.UTF8.GetBytes((saveId ?? string.Empty) + "|" + dayIndex);
            foreach (byte b in bytes)
            {
                h ^= b;
                h *= prime;
            }
            return h;
        }
    }

    /// <summary>docs/04 §11 取整。</summary>
    public static class Rounding
    {
        /// <summary>显示给玩家的整数：四舍五入，0.5 进一。</summary>
        public static int Display(double v) => (int)Math.Round(v, MidpointRounding.AwayFromZero);

        /// <summary>写进布料配置时保留的小数位。</summary>
        public static double Cloth(double v, BalanceData b) => Math.Round(v, b.clothDecimals, MidpointRounding.AwayFromZero);
    }
}
