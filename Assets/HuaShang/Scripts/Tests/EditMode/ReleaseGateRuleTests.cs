using System.Collections.Generic;
using NUnit.Framework;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Tests
{
    /// <summary>
    /// 发布门（docs/26 §1）第 4、6 条的规则部分。画面部分见 PlayMode 的 ReleaseGateTests。
    /// </summary>
    public class ReleaseGateRuleTests
    {
        static ConfigSnapshot C => Fixture.Config;

        static List<Beat> All(Beat b, int n)
        {
            var l = new List<Beat>();
            for (int i = 0; i < n; i++) l.Add(b);
            return l;
        }

        /// <summary>开局绢染靛蓝、裁上襦和裙；上襦按 beat 缝，裙稳缝，收成襦裙。</summary>
        public static SaveRoot GarmentWithUpperSewn(Beat upperBeat, out Garment garment, out GarmentPart upper)
        {
            var s = NewGameFactory.Create(C, "gate-save");
            Assert.IsTrue(Craft.ConfirmBolt(s, C, NewGameFactory.OpeningBoltId).ok);
            var dye = Craft.Dye(s, C, new Craft.DyeInput
            {
                boltId = NewGameFactory.OpeningBoltId, dyeId = "indigo", concentration = DyeCalc.Medium,
                temperature = Craft.TempWarm, liftSeconds = C.balance.dye.LiftCenterOf(DyeCalc.Medium),
                stirSteady = 1, stirOff = 0, stirTotal = 1,
            });
            Assert.IsTrue(dye.ok, dye.error);
            var u = Craft.Cut(s, C, new Craft.CutInput { boltId = NewGameFactory.OpeningBoltId, slot = "upper" });
            var k = Craft.Cut(s, C, new Craft.CutInput { boltId = NewGameFactory.OpeningBoltId, slot = "skirt" });
            Assert.IsTrue(u.ok && k.ok, u.error + k.error);
            int needles = C.balance.weave.sewNeedlesPerPart;
            Assert.IsTrue(Craft.Sew(s, C, u.createdId, All(upperBeat, needles)).ok);
            Assert.IsTrue(Craft.Sew(s, C, k.createdId, All(Beat.Steady, needles)).ok);
            var g = Craft.Assemble(s, C, new Craft.AssembleInput { pieceIds = new List<string> { u.createdId, k.createdId } });
            Assert.IsTrue(g.ok, "缝得再差也能收成成衣：" + g.error);
            garment = Find.Garment(s, g.createdId);
            upper = garment.parts.Find(p => p.slot == "upper");
            return s;
        }

        static string BoltsJson(SaveRoot s) => SaveSerializer.ToJson(new SaveRoot { schema = s.schema, bolts = s.bolts });

        [Test]
        public void 门4_缝制分低于阈值时拉伸取低端_成衣仍收得成()
        {
            var s = GarmentWithUpperSewn(Beat.Chaos, out var g, out var upper);
            Assert.IsTrue(upper.sewScore.HasValue);
            Assert.Less(upper.sewScore.Value, C.balance.penalties.sewLowThreshold, "全乱的针应压到阈值以下");
            var bolt = Find.Bolt(s, upper.boltId);
            var group = C.StretchGroup(C.Variety(bolt.variety).stretchGroup);
            var t = QualityCalc.T(ItemQuality.BoltQ(bolt, C) ?? 0, C.balance.quality);
            var low = ClothDescribe.FromLayers(C, bolt.variety, t, bolt.dynastyStyle, bolt.dyeLayers, bolt.finish, bolt.edgeDamage, upper.sewScore);
            Assert.AreEqual(group.distanceMin, low.stretch, 1e-9, "缝制失败覆盖拉伸：取该行低端（docs/04、docs/03 §2 第 7 步）");

            var skirt = g.parts.Find(p => p.slot == "skirt");
            var ok = ClothDescribe.FromLayers(C, bolt.variety, t, bolt.dynastyStyle, bolt.dyeLayers, bolt.finish, bolt.edgeDamage, skirt.sewScore);
            Assert.GreaterOrEqual(ok.stretch, group.distanceMin);
            Assert.LessOrEqual(ok.stretch, group.distanceMax);
            Assert.IsNotNull(g.tier, "低缝制分的成衣仍有档位");
        }

        [Test]
        public void 门6_呈现模式只改显示拷贝_仓库染层与cloth不变()
        {
            var s = GarmentWithUpperSewn(Beat.Steady, out var g, out var upper);
            var bolt = Find.Bolt(s, upper.boltId);
            ClothSync.RecomputeAll(s, C); // 运行时每条命令后都会重算，先对齐
            string before = SaveSerializer.ToJson(s);
            var standard = ClothDescribe.FromLayers(C, bolt.variety, 0.5, bolt.dynastyStyle, bolt.dyeLayers, bolt.finish, bolt.edgeDamage, upper.sewScore);
            foreach (var mode in new[] { ClothDescribe.ModeHistory, ClothDescribe.ModeStandard, ClothDescribe.ModeEnhanced })
            {
                var shown = ClothDescribe.ForPresentation(C, mode, bolt.variety, 0.5, bolt.dynastyStyle, bolt.dyeLayers, bolt.finish, bolt.edgeDamage, upper.sewScore);
                Assert.IsNotNull(shown);
                s.presentMode = mode;
                Assert.IsTrue(Stage.Record(s, C, "xiShi", 0, g.id).ok);
            }
            s.presentMode = SaveSerializer.FromJson(before).presentMode;
            ClothSync.RecomputeAll(s, C);
            var after = SaveSerializer.FromJson(SaveSerializer.ToJson(s));
            var was = SaveSerializer.FromJson(before);
            Assert.AreEqual(BoltsJson(was), BoltsJson(after), "染层与 cloth 不随呈现模式改变");
            Assert.AreEqual(standard.gloss, ClothDescribe.ForPresentation(C, ClothDescribe.ModeStandard, bolt.variety, 0.5, bolt.dynastyStyle, bolt.dyeLayers, bolt.finish, bolt.edgeDamage, upper.sewScore).gloss, 1e-12, "标准模式即仓库原样");
        }
    }
}
