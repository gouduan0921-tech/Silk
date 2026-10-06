using System;
using System.Collections.Generic;
using HuaShang.Rules.Config;

namespace HuaShang.Rules.Calc
{
    /// <summary>docs/21 §4 的一层染。存档与规则共用这一结构。</summary>
    [Serializable]
    public class DyeLayer
    {
        public string dyeId;
        /// <summary>light / medium / strong（docs/21 §4）。</summary>
        public string concentration;
        public double strength;
        public double uneven;
        /// <summary>这一层的染色分（docs/21 §4）。</summary>
        public double score;
        public string maskId;
        /// <summary>防染（docs/04 §5）：tie 扎染 / clamp 夹缬；空为整匹浸染。</summary>
        public string resist;
    }

    public class AddLayerResult
    {
        public List<DyeLayer> layers;
        /// <summary>挤掉最早一层时的染色分扣减（负数）；没有挤掉则为 0。</summary>
        public int scorePenalty;
        public DyeLayer removed;
    }

    /// <summary>docs/04 §5 染色公式与 docs/07 §2。</summary>
    public static class DyeCalc
    {
        public const string Light = "light";
        public const string Medium = "medium";
        public const string Strong = "strong";

        /// <summary>强度 = min(上限, 浓度系数 × 时间吻合 × 温度吻合)。吻合度取 0–1。</summary>
        public static double Strength(string concentrationKey, double timeMatch, double tempMatch, DyeData d)
        {
            double raw = d.ConcentrationOf(concentrationKey) * Clamp01(timeMatch) * Clamp01(tempMatch);
            return Math.Min(d.strengthCap, raw);
        }

        /// <summary>染色分 = 温度吻合×w + 时间吻合×w + 搅拌稳定×w。吻合度取 0–1。</summary>
        public static double Score(double tempMatch, double timeMatch, double stirStability, DyeData d)
        {
            return Clamp01(tempMatch) * d.scoreTemp
                 + Clamp01(timeMatch) * d.scoreTime
                 + Clamp01(stirStability) * d.scoreStir;
        }

        /// <summary>新颜色 = 旧颜色 × (1 − 强度×k) + 染料色 × (强度×k)，逐通道。</summary>
        public static double MixChannel(double oldValue, double dyeValue, double strength, DyeData d)
        {
            double k = strength * d.colorMix;
            return oldValue * (1 - k) + dyeValue * k;
        }

        /// <summary>
        /// 叠一层。超过上限时挤掉最早一层并扣染色分（N5）。不改动传入的列表。
        /// </summary>
        public static AddLayerResult AddLayer(IList<DyeLayer> existing, DyeLayer layer, DyeData d)
        {
            var list = existing == null ? new List<DyeLayer>() : new List<DyeLayer>(existing);
            list.Add(layer);
            var result = new AddLayerResult { layers = list };
            if (list.Count > d.maxLayers)
            {
                result.removed = list[0];
                list.RemoveAt(0);
                result.scorePenalty = d.overflowPenalty;
            }
            return result;
        }

        /// <summary>透明度系数 = 1 − k × 层数 × 平均强度。染色不提高透明度，因此不超过 1。</summary>
        public static double OpacityFactor(IList<DyeLayer> layers, DyeData d)
        {
            if (layers == null || layers.Count == 0) return 1;
            double sum = 0;
            foreach (var l in layers) sum += l.strength;
            double avg = sum / layers.Count;
            return Math.Min(1, Math.Max(0, 1 - d.opacityPerLayer * layers.Count * avg));
        }

        static double Clamp01(double v) => Math.Max(0, Math.Min(1, v));
    }
}
