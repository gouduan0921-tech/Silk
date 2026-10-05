using System;
using HuaShang.Rules.Config;

namespace HuaShang.Rules.Calc
{
    /// <summary>成衣七项过程分。null 表示该工序没有分数。</summary>
    public class ProcessScores
    {
        public double? material;
        public double? yarn;
        public double? weave;
        /// <summary>未染色时为 null，按 docs/04 §4 的默认分计。</summary>
        public double? dye;
        /// <summary>未后整理时为 null，按 docs/04 §4 的默认分计。</summary>
        public double? finish;
        public double? cut;
        public double? sew;
    }

    /// <summary>docs/04 §4 与 docs/13 §1。档位只决定在区间里取哪个位置。</summary>
    public static class QualityCalc
    {
        public const string Common = "common";
        public const string Fine = "fine";
        public const string Legendary = "legendary";

        /// <summary>
        /// 计算 Q。原料、成纱、织造、裁剪、缝制缺任何一项时返回 false，不猜分数（构架文档 G3）。
        /// 染色与后整理缺项用 docs/04 §4 的默认分，不当成 0（N1）。
        /// </summary>
        public static bool TryComputeQ(ProcessScores s, QualityData q, out double result)
        {
            result = 0;
            if (s == null || q == null) return false;
            if (!s.material.HasValue || !s.yarn.HasValue || !s.weave.HasValue
                || !s.cut.HasValue || !s.sew.HasValue)
                return false;

            double dye = s.dye ?? q.undyedDyeScore;
            double finish = s.finish ?? q.unfinishedFinishScore;
            var w = q.weights;
            result = s.material.Value * w.material
                   + s.yarn.Value * w.yarn
                   + s.weave.Value * w.weave
                   + dye * w.dye
                   + finish * w.finish
                   + s.cut.Value * w.cut
                   + s.sew.Value * w.sew;
            return true;
        }

        /// <summary>
        /// 布匹 Q（docs/04 §4）：只取原料、成纱、织造、染色、后整理五项，权重归一。
        /// 原料、成纱、织造缺任一项时返回 false。
        /// </summary>
        public static bool TryComputeBoltQ(double? material, double? yarn, double? weave, double? dye, double? finish,
                                           QualityData q, out double result)
        {
            result = 0;
            if (!material.HasValue || !yarn.HasValue || !weave.HasValue) return false;
            var w = q.weights;
            double sum = w.material + w.yarn + w.weave + w.dye + w.finish;
            if (sum <= 0) return false;
            result = (material.Value * w.material + yarn.Value * w.yarn + weave.Value * w.weave
                      + (dye ?? q.undyedDyeScore) * w.dye + (finish ?? q.unfinishedFinishScore) * w.finish) / sum;
            return true;
        }

        public static TierBand BandOf(double qValue, QualityData q)
        {
            if (q.bands == null || q.bands.Count == 0)
                throw new InvalidOperationException("docs/04 §4 档位表为空");
            TierBand found = q.bands[0];
            foreach (var b in q.bands)
                if (qValue >= b.qMin) found = b;
            return found;
        }

        public static string TierOf(double qValue, QualityData q) => BandOf(qValue, q).tier;

        /// <summary>区间位置 t，按所在档的公式算，再夹到 [0, 1]。</summary>
        public static double T(double qValue, QualityData q)
        {
            var b = BandOf(qValue, q);
            double clampedQ = Math.Max(b.qMin, Math.Min(b.qMax, qValue));
            double t = b.tBase + (clampedQ - b.qOffset) / b.denominator * b.span;
            return Math.Max(0, Math.Min(1, t));
        }
    }
}
