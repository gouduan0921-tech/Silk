using System;
using System.Collections.Generic;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>物品的分数、档位与词条，公式全部来自 docs/04 §4、§6（结构见 docs/13）。</summary>
    public static class ItemQuality
    {
        /// <summary>布匹染色分 = 各层 score 平均 + dyePenalty，夹在 0–100；未染为 null（docs/21 §4）。</summary>
        public static double? BoltDyeScore(Bolt b)
        {
            if (b.dyeLayers == null || b.dyeLayers.Count == 0) return null;
            double sum = 0;
            foreach (var l in b.dyeLayers) sum += l.score;
            return Math.Max(0, Math.Min(100, sum / b.dyeLayers.Count + b.dyePenalty));
        }

        public static double? BoltQ(Bolt b, ConfigSnapshot c)
        {
            if (QualityCalc.TryComputeBoltQ(b.materialScore, b.yarnScore, b.weaveScore, BoltDyeScore(b), b.finishScore, c.balance.quality, out var q))
                return q;
            return null;
        }

        /// <summary>布匹阶段取 cloth 的 t；分数不全时取普通档低端（t = 0），不猜分。</summary>
        public static double BoltT(Bolt b, ConfigSnapshot c)
        {
            var q = BoltQ(b, c);
            return q.HasValue ? QualityCalc.T(q.Value, c.balance.quality) : 0;
        }

        public static bool IsDefect(Bolt b, ConfigSnapshot c) => b.weaveScore.HasValue && b.weaveScore.Value < c.balance.weave.defectLine;

        /// <summary>槽位对应的层（docs/05 §4 槽位；披帛算外层附件，docs/04 §4）。</summary>
        public static string LayerOf(string slot)
        {
            switch (slot)
            {
                case "inner": return "inner";
                case "wrap": return "middle";
                case "drape": return "drape";
                default: return "outer";
            }
        }

        /// <summary>按层算成衣 Q：每层七项取该层部件的平均，再按外 / 中 / 里权重归一（docs/04 §4）。</summary>
        public static bool TryGarmentQ(SaveRoot s, ConfigSnapshot c, IList<GarmentPart> parts, out double q, out GarmentScores outerScores)
        {
            q = 0;
            outerScores = null;
            var qd = c.balance.quality;
            double weighted = 0, weightSum = 0;
            foreach (var layer in new[] { "outer", "middle", "inner" })
            {
                var group = new List<GarmentPart>();
                foreach (var p in parts)
                {
                    string l = LayerOf(p.slot);
                    if (l == layer || (layer == "outer" && l == "drape")) group.Add(p);
                }
                if (group.Count == 0) continue;
                var sc = LayerScores(s, group);
                if (layer == "outer") outerScores = sc;
                var ps = new ProcessScores { material = sc.material, yarn = sc.yarn, weave = sc.weave, dye = sc.dye, finish = sc.finish, cut = sc.cut, sew = sc.sew };
                if (!QualityCalc.TryComputeQ(ps, qd, out var lq)) return false;
                double w = layer == "outer" ? qd.layerOuter : layer == "middle" ? qd.layerMiddle : qd.layerInner;
                weighted += lq * w;
                weightSum += w;
            }
            if (weightSum <= 0) return false;
            q = weighted / weightSum;
            return true;
        }

        public static GarmentScores LayerScores(SaveRoot s, IList<GarmentPart> parts)
        {
            var sc = new GarmentScores();
            sc.material = Avg(parts, p => Find.Bolt(s, p.boltId)?.materialScore);
            sc.yarn = Avg(parts, p => Find.Bolt(s, p.boltId)?.yarnScore);
            sc.weave = Avg(parts, p => Find.Bolt(s, p.boltId)?.weaveScore);
            sc.dye = Avg(parts, p => { var b = Find.Bolt(s, p.boltId); return b == null ? (double?)null : BoltDyeScore(b); });
            sc.finish = Avg(parts, p => Find.Bolt(s, p.boltId)?.finishScore);
            sc.cut = Avg(parts, p => p.cutScore);
            sc.sew = Avg(parts, p => p.sewScore);
            return sc;
        }

        static double? Avg<T>(IList<GarmentPart> parts, Func<GarmentPart, T?> get) where T : struct, IConvertible
        {
            double sum = 0; int n = 0;
            foreach (var p in parts)
            {
                var v = get(p);
                if (v.HasValue) { sum += v.Value.ToDouble(null); n++; }
            }
            return n == 0 ? (double?)null : sum / n;
        }

        /// <summary>
        /// 词条（docs/04 §6）：最多 maxTraits 个。蝉翼、流光、活色、层叠、合制按表判定；越代需要朝代典型表，首发不产生。
        /// </summary>
        public static List<string> Traits(SaveRoot s, ConfigSnapshot c, Garment g, GarmentScores outer, PatternRow pattern, bool extremeSize)
        {
            var list = new List<string>();
            var a = c.balance.affection;
            int Th(string name, int i = 0)
            {
                var t = a.traits.Find(x => x.name == name);
                return t != null && t.thresholds.Count > i ? t.thresholds[i] : int.MaxValue;
            }
            var outerParts = g.parts.FindAll(p => LayerOf(p.slot) == "outer");
            var outerBolt = outerParts.Count > 0 ? Find.Bolt(s, outerParts[0].boltId) : null;
            var variety = outerBolt != null ? c.varieties.Find(v => v.id == outerBolt.variety) : null;

            // 蝉翼：素纱或花纱，原料分和织造分都 ≥ 线
            if (variety != null && (variety.id == "suSha" || variety.id == "huaSha")
                && outer.material >= Th("蝉翼") && outer.weave >= Th("蝉翼")) list.Add("蝉翼");
            // 流光：织造分 ≥ 线，且组织是缎或锦
            if (variety != null && (variety.name.Contains("缎") || variety.name.Contains("锦")) && outer.weave >= Th("流光")) list.Add("流光");
            // 活色：不均落在配方允许范围内且不是失误色花
            if (outerBolt != null && outerBolt.dyeLayers.Exists(l => l.uneven >= c.balance.dye.livelyMin && l.uneven <= c.balance.dye.unevenAllowedMax)
                && !outerBolt.dyeLayers.Exists(l => l.uneven > c.balance.dye.unevenAllowedMax)) list.Add("活色");
            // 层叠：缝制分 ≥ 线，且成衣至少两层
            var layers = new HashSet<string>();
            foreach (var p in g.parts) { var l = LayerOf(p.slot); if (l != "drape") layers.Add(l); }
            var allSew = LayerScores(s, g.parts).sew;
            if (layers.Count >= 2 && allSew >= Th("层叠")) list.Add("层叠");
            // 合制：朝代与形制匹配，裁剪分 ≥ 线；衣长袖宽选极端档拿不到（docs/07 §3）
            if (pattern != null && g.dynastyStyle == pattern.dynasty && !extremeSize && outer.cut >= Th("合制")) list.Add("合制");

            if (list.Count > a.maxTraits) list.RemoveRange(a.maxTraits, list.Count - a.maxTraits);
            return list;
        }
    }
}
