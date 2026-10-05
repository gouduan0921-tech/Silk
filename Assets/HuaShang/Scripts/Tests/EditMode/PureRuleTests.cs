using System;
using System.Collections.Generic;
using NUnit.Framework;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;

namespace HuaShang.Tests
{
    /// <summary>docs/26 §2 的纯函数断言。P0 退出条件：N1–N6。N12 一并提前覆盖。</summary>
    public class PureRuleTests
    {
        static ConfigSnapshot C => Fixture.Config;
        const double Eps = 1e-9;

        static ProcessScores AllButDyeAndFinish(double v)
        {
            return new ProcessScores { material = v, yarn = v, weave = v, cut = v, sew = v };
        }

        [Test]
        public void N1_未染色时染色分按默认分而不是0()
        {
            var q = C.balance.quality;
            Assert.AreNotEqual(0, q.undyedDyeScore, "docs/04 §4 的未染默认分不应为 0");

            var undyed = AllButDyeAndFinish(80);
            Assert.IsTrue(QualityCalc.TryComputeQ(undyed, q, out var qUndyed));

            var withDefault = AllButDyeAndFinish(80);
            withDefault.dye = q.undyedDyeScore;
            withDefault.finish = q.unfinishedFinishScore;
            Assert.IsTrue(QualityCalc.TryComputeQ(withDefault, q, out var qDefault));
            Assert.AreEqual(qDefault, qUndyed, Eps);

            var zero = AllButDyeAndFinish(80);
            zero.dye = 0;
            zero.finish = q.unfinishedFinishScore;
            Assert.IsTrue(QualityCalc.TryComputeQ(zero, q, out var qZero));
            Assert.Greater(qUndyed, qZero);
        }

        [Test]
        public void 缺原料分时不计算Q()
        {
            var s = AllButDyeAndFinish(80);
            s.material = null;
            Assert.IsFalse(QualityCalc.TryComputeQ(s, C.balance.quality, out _));
        }

        [Test]
        public void N2_精良区间两端都是精良且t取到两端()
        {
            var q = C.balance.quality;
            var fine = q.bands.Find(b => b.tier == QualityCalc.Fine);
            Assert.IsNotNull(fine, "docs/04 §4 应有精良档");

            Assert.AreEqual(QualityCalc.Fine, QualityCalc.TierOf(fine.qMin, q));
            Assert.AreEqual(QualityCalc.Fine, QualityCalc.TierOf(fine.qMax, q));
            Assert.AreNotEqual(QualityCalc.Fine, QualityCalc.TierOf(fine.qMin - 1, q));
            Assert.AreNotEqual(QualityCalc.Fine, QualityCalc.TierOf(fine.qMax + 1, q));

            Assert.AreEqual(fine.tBase, QualityCalc.T(fine.qMin, q), Eps);
            Assert.AreEqual(fine.tBase + fine.span, QualityCalc.T(fine.qMax, q), Eps);
        }

        [Test]
        public void N3_汉偏移后密度不出该行区间()
        {
            var han = C.balance.quality.dynasties.Find(d => d.launch);
            Assert.IsNotNull(han, "docs/04 §4 应有首发朝代偏移");
            foreach (var v in C.varieties)
            {
                if (!v.launch) continue;
                foreach (double t in new[] { 0, 0.25, 0.5, 0.75, 1 })
                {
                    var d = ClothRecipe.Compute(new ClothInput { varietyId = v.id, t = t, dynasty = han.dynasty }, C);
                    Assert.GreaterOrEqual(d.density, v.densityMin - Eps, v.id + " t=" + t);
                    Assert.LessOrEqual(d.density, v.densityMax + Eps, v.id + " t=" + t);
                    Assert.GreaterOrEqual(d.bend, v.bendMin - Eps, v.id + " t=" + t);
                    Assert.LessOrEqual(d.bend, v.bendMax + Eps, v.id + " t=" + t);
                    Assert.GreaterOrEqual(d.wind, v.windMin - Eps, v.id + " t=" + t);
                    Assert.LessOrEqual(d.wind, v.windMax + Eps, v.id + " t=" + t);
                }
            }
        }

        [Test]
        public void N4_浓染强度不超过上限()
        {
            var d = C.balance.dye;
            Assert.LessOrEqual(DyeCalc.Strength(DyeCalc.Strong, 1, 1, d), d.strengthCap + Eps);
            Assert.LessOrEqual(DyeCalc.Strength(DyeCalc.Strong, 5, 5, d), d.strengthCap + Eps);
            Assert.AreEqual(Math.Min(d.strengthCap, d.ConcentrationOf(DyeCalc.Strong)),
                            DyeCalc.Strength(DyeCalc.Strong, 1, 1, d), Eps);
        }

        [Test]
        public void N5_超过层数上限时挤掉第一层并扣分()
        {
            var d = C.balance.dye;
            var layers = new List<DyeLayer>();
            AddLayerResult last = null;
            for (int i = 0; i <= d.maxLayers; i++)
            {
                last = DyeCalc.AddLayer(layers, new DyeLayer { dyeId = "indigo", strength = 0.3, maskId = "m" + i }, d);
                if (i < d.maxLayers) Assert.AreEqual(0, last.scorePenalty, "未超上限不扣分");
                layers = last.layers;
            }
            Assert.AreEqual(d.maxLayers, layers.Count);
            Assert.AreEqual("m0", last.removed.maskId);
            Assert.AreEqual("m1", layers[0].maskId);
            Assert.AreEqual(d.overflowPenalty, last.scorePenalty);
            Assert.Less(last.scorePenalty, 0);
        }

        [Test]
        public void N6_边损到门槛不能做传世外层()
        {
            var p = C.balance.penalties;
            Assert.IsFalse(LayerRules.CanBeLegendaryOuter(p.edgeNoLegendaryOuter, p));
            Assert.IsTrue(LayerRules.CanBeLegendaryOuter(p.edgeNoLegendaryOuter - 1, p));
        }

        [Test]
        public void N12_取消工序工时向上取整且有下限()
        {
            var day = C.balance.day;
            Assert.Greater(day.hours.Count, 0);
            foreach (var h in day.hours)
            {
                int cost = HoursCalc.CancelCost(h.hours, day);
                Assert.GreaterOrEqual(cost, day.cancelMinHours, h.label);
                Assert.AreEqual(Math.Max(day.cancelMinHours, (int)Math.Ceiling(h.hours / 2.0)), cost, h.label);
            }
        }

        [Test]
        public void 工时账不做教学豁免()
        {
            var day = C.balance.day;
            int used = day.HoursOf("dyeBath") + 2 * day.HoursOf("cutPart") + 2 * day.HoursOf("sewPart");
            // 构架文档 G1：首日只做上襦和裙已超过一天工时时，必须如实报告不足。
            bool fits = used <= day.hoursPerDay;
            Assert.AreEqual(fits, HoursCalc.CanSpend(0, used, day));
        }

        [Test]
        public void 随机哈希稳定()
        {
            Assert.AreEqual(StableHash.Of("save-1", 3), StableHash.Of("save-1", 3));
            Assert.AreNotEqual(StableHash.Of("save-1", 3), StableHash.Of("save-1", 4));
        }
    }
}
