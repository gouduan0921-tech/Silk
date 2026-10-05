using System;
using HuaShang.Rules.Config;

namespace HuaShang.Rules.Calc
{
    /// <summary>
    /// 纯函数产出的 cloth 描述。只有解算模块把它写进 MagicaCloth 2 的 SerializeData。
    /// 字段对照见 docs/03 §2。
    /// </summary>
    [Serializable]
    public class ClothDescriptor
    {
        public double density;   // gravity
        public double bend;      // angleRestorationConstraint.stiffness
        public double wind;      // wind.influence
        public double damping;   // damping
        public double stretch;   // distanceConstraint.stiffness
        public double gloss;     // URP 材质（10 分制）
        public double opacity;   // URP 材质（10 分制）
        /// <summary>docs/04 §5 透明度系数，乘在材质透明度上。</summary>
        public double opacityFactor = 1;
    }

    /// <summary>ClothRecipe 的输入。染层的统计由调用方给出，规则只认计数。</summary>
    public class ClothInput
    {
        public string varietyId;
        /// <summary>按 QualityCalc.T 求得的区间位置。</summary>
        public double t;
        public string dynasty;
        /// <summary>植物染层数。</summary>
        public int plantLayers;
        /// <summary>其中按「浓」染的层数。docs/21 的染层未记浓度，见构架文档 G7。</summary>
        public int strongLayers;
        public bool vitriol;
        public bool calendered;
        public double averageStrength;
        public int edgeDamage;
        /// <summary>未缝为 null。</summary>
        public double? sewScore;
    }

    /// <summary>
    /// docs/03 §2 的顺序：
    /// 1 取品种区间 → 2 按 t 取位置 → 3 乘朝代偏移 → 4 夹回区间 → 5 加染色与后整理修正并再夹回
    /// → 6 金线掩膜（首发不出现，未实现） → 7 边损与缝制失败覆盖拉伸。
    /// 所有数都来自 ConfigSnapshot。
    /// </summary>
    public static class ClothRecipe
    {
        public static ClothDescriptor Compute(ClothInput input, ConfigSnapshot c)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var v = c.Variety(input.varietyId);
            var g = c.StretchGroup(v.stretchGroup);
            var off = c.Dynasty(input.dynasty);
            if (!off.parsed)
                throw new InvalidOperationException("朝代偏移 " + input.dynasty + " 在 docs/04 §4 中无法按倍数读出，不能使用");
            var b = c.balance;
            double t = Math.Max(0, Math.Min(1, input.t));

            // 1–2
            var d = new ClothDescriptor
            {
                density = Lerp(v.densityMin, v.densityMax, t),
                bend = Lerp(v.bendMin, v.bendMax, t),
                wind = Lerp(v.windMin, v.windMax, t),
                gloss = Lerp(v.glossMin, v.glossMax, t),
                opacity = Lerp(v.opacityMin, v.opacityMax, t),
                damping = Lerp(g.dampingMin, g.dampingMax, t),
                stretch = Lerp(g.distanceMin, g.distanceMax, t),
            };

            // 3 朝代偏移（乘在已取值上，夹紧前）
            d.density *= off.density;
            d.bend *= off.bend;
            d.wind *= off.wind;
            d.gloss *= off.gloss;

            // 4 夹回该行区间
            ClampToRow(d, v, g);

            // 5 染色与后整理修正，加完再夹回
            var dye = b.dye;
            if (input.plantLayers > 0)
            {
                Apply(d, dye.ModOf("plantLayer"), input.plantLayers);
                Apply(d, dye.ModOf("fromSecondLayer"), Math.Max(0, input.plantLayers - 1));
            }
            if (input.strongLayers > 0) Apply(d, dye.ModOf("strongExtra"), input.strongLayers);
            if (input.vitriol) Apply(d, dye.ModOf("vitriol"), 1);
            if (input.calendered) Apply(d, dye.ModOf("calender"), 1);
            ClampToRow(d, v, g);

            int layers = input.plantLayers;
            d.opacityFactor = layers == 0 ? 1 : Math.Min(1, Math.Max(0, 1 - dye.opacityPerLayer * layers * input.averageStrength));

            // 7 边损与缝制失败覆盖拉伸
            var p = b.penalties;
            bool lowSew = input.sewScore.HasValue && input.sewScore.Value < p.sewLowThreshold;
            if (lowSew || input.edgeDamage > 0) d.stretch = g.distanceMin;
            if (input.edgeDamage > 0)
            {
                d.gloss += p.edgeGlossPerPoint * input.edgeDamage;
                d.gloss = Clamp(d.gloss, v.glossMin, v.glossMax);
            }

            d.density = Rounding.Cloth(d.density, b);
            d.bend = Rounding.Cloth(d.bend, b);
            d.wind = Rounding.Cloth(d.wind, b);
            d.damping = Rounding.Cloth(d.damping, b);
            d.stretch = Rounding.Cloth(d.stretch, b);
            d.gloss = Rounding.Cloth(d.gloss, b);
            d.opacity = Rounding.Cloth(d.opacity, b);
            d.opacityFactor = Rounding.Cloth(d.opacityFactor, b);
            return d;
        }

        static void Apply(ClothDescriptor d, TreatmentMod m, int times)
        {
            for (int i = 0; i < times; i++)
            {
                d.damping += m.damping;
                d.bend = d.bend * m.bendFactor + m.bend;
                d.gloss += m.gloss;
            }
        }

        static void ClampToRow(ClothDescriptor d, VarietyRow v, StretchGroupRow g)
        {
            d.density = Clamp(d.density, v.densityMin, v.densityMax);
            d.bend = Clamp(d.bend, v.bendMin, v.bendMax);
            d.wind = Clamp(d.wind, v.windMin, v.windMax);
            d.gloss = Clamp(d.gloss, v.glossMin, v.glossMax);
            d.opacity = Clamp(d.opacity, v.opacityMin, v.opacityMax);
            d.damping = Clamp(d.damping, g.dampingMin, g.dampingMax);
            d.stretch = Clamp(d.stretch, g.distanceMin, g.distanceMax);
        }

        static double Lerp(double a, double b, double t) => a + (b - a) * t;
        static double Clamp(double x, double lo, double hi) => Math.Max(lo, Math.Min(hi, x));
    }
}
