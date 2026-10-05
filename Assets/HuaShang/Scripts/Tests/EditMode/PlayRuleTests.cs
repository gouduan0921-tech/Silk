using System.Collections.Generic;
using NUnit.Framework;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Tests
{
    /// <summary>玩法命令：首日物品去向、F1–F5、N7–N11、S2–S3（docs/26）。</summary>
    public class PlayRuleTests
    {
        static ConfigSnapshot C => Fixture.Config;

        static SaveRoot NewGame() => NewGameFactory.Create(C, "test-save");

        static List<Beat> Steady(int n)
        {
            var l = new List<Beat>();
            for (int i = 0; i < n; i++) l.Add(Beat.Steady);
            return l;
        }

        static Craft.DyeInput GoodIndigo(string boltId) => new Craft.DyeInput
        {
            boltId = boltId,
            dyeId = "indigo",
            concentration = DyeCalc.Medium,
            temperature = Craft.TempWarm,
            liftSeconds = C.balance.dye.LiftCenterOf(DyeCalc.Medium),
            stirSteady = 8, stirOff = 0, stirTotal = 8,
        };

        /// <summary>首日：与界面样板一致的物品去向（构架文档 §1.3）。</summary>
        public static SaveRoot PlayFirstDay(out string garmentId, out string drapeId)
        {
            var s = NewGame();
            Assert.IsTrue(Craft.ConfirmBolt(s, C, NewGameFactory.OpeningBoltId).ok);
            var d = Craft.Dye(s, C, GoodIndigo(NewGameFactory.OpeningBoltId));
            Assert.IsTrue(d.ok, d.error);
            var upper = Craft.Cut(s, C, new Craft.CutInput { boltId = NewGameFactory.OpeningBoltId, slot = "upper" });
            var skirt = Craft.Cut(s, C, new Craft.CutInput { boltId = NewGameFactory.OpeningBoltId, slot = "skirt" });
            var drape = Craft.Cut(s, C, new Craft.CutInput { boltId = NewGameFactory.OpeningBoltId, slot = "drape" });
            Assert.IsTrue(upper.ok && skirt.ok && drape.ok, upper.error + skirt.error + drape.error);
            int needles = C.balance.weave.sewNeedlesPerPart;
            Assert.IsTrue(Craft.Sew(s, C, upper.createdId, Steady(needles)).ok);
            Assert.IsTrue(Craft.Sew(s, C, skirt.createdId, Steady(needles)).ok);
            var g = Craft.Assemble(s, C, new Craft.AssembleInput { pieceIds = new List<string> { upper.createdId, skirt.createdId } });
            Assert.IsTrue(g.ok, g.error);
            var perf = Stage.Record(s, C, "xiShi", 0, g.createdId);
            Assert.IsTrue(perf.ok, perf.error);
            Assert.IsTrue(Exhibits.Place(s, C, 0, "garment", g.createdId).ok);
            garmentId = g.createdId;
            drapeId = drape.createdId;
            return s;
        }

        [Test]
        public void F1_首日物品去向与样板一致()
        {
            var s = PlayFirstDay(out var garmentId, out var drapeId);
            var boltA = Find.Bolt(s, NewGameFactory.OpeningBoltId);
            double used = 0;
            foreach (var slot in new[] { "upper", "skirt", "drape" })
                used += C.patterns.Find(p => p.id == "ruQun").partLengths.Find(x => x.slot == slot).length;
            Assert.AreEqual(C.opening.bolt.length - used, boltA.length, 1e-6, "余绢");
            Assert.AreEqual(1, boltA.dyeLayers.Count);
            Assert.AreEqual("indigo", boltA.dyeLayers[0].dyeId);
            Assert.AreEqual(C.opening.yarn.length, Find.Yarn(s, NewGameFactory.OpeningYarnId).length, 1e-6, "细丝 B 未织");
            var indigoStart = C.opening.dyes.Find(x => x.dyeId == "indigo").count;
            Assert.AreEqual(indigoStart - C.balance.dye.costPerBolt, Find.Dye(s, "indigo").count);
            Assert.AreEqual(C.opening.dyes.Find(x => x.dyeId == "madder").count, Find.Dye(s, "madder").count);
            Assert.AreEqual(C.opening.silkCoin, s.silkCoin, "单柜预览不发丝钱");
            var drape = Find.Piece(s, drapeId);
            Assert.IsNotNull(drape, "披帛留在侧架");
            Assert.IsNull(drape.sewScore, "披帛未缝");
            Assert.IsTrue(s.exhibit.slots.Exists(x => x.itemId == garmentId));
            Assert.IsTrue(Find.Quest(s, Craft.Tutorial1).done && Find.Quest(s, Craft.Tutorial2).done && Find.Quest(s, Craft.Tutorial3).done, "教学三步按 docs/10 顺序完成");
            Assert.LessOrEqual(s.hoursUsed, C.balance.day.HoursLimit(0), "首日在教学日工时内完成（docs/04 §2）");
            Assert.IsNotNull(Find.Garment(s, garmentId).tier, "开局物品分数齐全，首件成衣能算出档位");
        }

        [Test]
        public void 工时不够时如实拒绝()
        {
            var s = NewGame();
            s.hoursUsed = C.balance.day.HoursLimit(0);
            var r = Craft.Dye(s, C, GoodIndigo(NewGameFactory.OpeningBoltId));
            Assert.IsFalse(r.ok);
            Assert.AreEqual(C.opening.dyes.Find(x => x.dyeId == "indigo").count, Find.Dye(s, "indigo").count, "失败时不扣料");
        }

        [Test]
        public void F2_染坏仍能做里层()
        {
            var s = NewGame();
            var bad = GoodIndigo(NewGameFactory.OpeningBoltId);
            bad.stirSteady = 0; bad.stirOff = 0; bad.stirTotal = 8;
            var d = Craft.Dye(s, C, bad);
            Assert.IsTrue(d.ok, d.error);
            var bolt = Find.Bolt(s, NewGameFactory.OpeningBoltId);
            Assert.IsNotNull(bolt.dyeLayers[0].maskId, "色花写成掩膜，不删布（docs/07 §2）");
            var inner = Craft.Cut(s, C, new Craft.CutInput { boltId = bolt.id, slot = "inner" });
            Assert.IsTrue(inner.ok, inner.error);
        }

        [Test]
        public void F3_拆衣后染色还在边损增加()
        {
            var s = PlayFirstDay(out var garmentId, out _);
            var bolt = Find.Bolt(s, NewGameFactory.OpeningBoltId);
            int edge = bolt.edgeDamage;
            double len = bolt.length;
            Assert.IsTrue(Craft.Unpick(s, C, garmentId).ok);
            Assert.AreEqual(1, bolt.dyeLayers.Count);
            Assert.AreEqual(edge + C.balance.penalties.unpickEdgeDamage, bolt.edgeDamage);
            Assert.Greater(bolt.length, len);
            Assert.IsFalse(s.exhibit.slots.Exists(x => x.itemId == garmentId), "拆掉的成衣下架");
        }

        [Test]
        public void F4_西施30档在好感到门槛前不可选()
        {
            var s = NewGame();
            int t30 = C.balance.affection.tierThresholds[1];
            CollectionAssert.DoesNotContain(Stage.AvailableTiers(s, C, "xiShi"), t30);
            Find.Character(s, "xiShi").affection = t30;
            CollectionAssert.Contains(Stage.AvailableTiers(s, C, "xiShi"), t30);
        }

        [Test]
        public void 关闭角色与未到等级的角色不出现在上场门()
        {
            var s = NewGame();
            var door = Stage.StageDoor(s, C, null);
            CollectionAssert.AreEquivalent(new[] { "xiShi" }, door);
            s.level = 3;
            door = Stage.StageDoor(s, C, null);
            CollectionAssert.AreEquivalent(new[] { "xiShi", "wangZhaoJun", "zhaoFeiYan" }, door);
            foreach (var ch in C.characters)
                if (!ch.enabled) CollectionAssert.DoesNotContain(door, ch.id);
            CollectionAssert.AreEqual(new[] { 0 }, Stage.AvailableTiers(s, C, "zhaoFeiYan"));
        }

        [Test]
        public void F5_单柜预览不发丝钱_两件开幕后才发()
        {
            var s = PlayFirstDay(out _, out _);
            Assert.AreEqual(0, Exhibits.SettleToday(s, C, out _));
            Assert.IsNull(s.exhibit.lastPaidDay, "单柜预览不写结算日");
            Assert.IsTrue(Exhibits.Place(s, C, 1, "bolt", NewGameFactory.OpeningBoltId).ok);
            Exhibits.EditLabel(s, 0, "靛蓝襦裙", true, true, true);
            Exhibits.EditLabel(s, 1, "余绢", true, true, true);
            int coins = Exhibits.SettleToday(s, C, out double score);
            Assert.Greater(score, 0);
            Assert.AreEqual(System.Math.Min(C.balance.economy.exhibitCoinCap, (int)System.Math.Floor(score / C.balance.economy.exhibitPointsPer) * C.balance.economy.exhibitCoinsPer), coins);
            Assert.Greater(coins, 0);
            Assert.AreEqual(0, Exhibits.SettleToday(s, C, out _), "N10：同一天第二次结算为 0");
        }

        [Test]
        public void 说明牌标题不超过40字()
        {
            var s = PlayFirstDay(out _, out _);
            Assert.IsTrue(Exhibits.EditLabel(s, 0, new string('丝', Exhibits.TitleMaxChars), true, false, false).ok);
            Assert.IsFalse(Exhibits.EditLabel(s, 0, new string('丝', Exhibits.TitleMaxChars + 1), true, false, false).ok);
        }

        [Test]
        public void N7_零档热度低于同衣三十档()
        {
            var s = PlayFirstDay(out var gid, out _);
            var g = Find.Garment(s, gid);
            ItemQuality.TryGarmentQ(s, C, g.parts, out var q, out _);
            int fit = Stage.Fit(s, C, g, "xiShi");
            Assert.Less(Stage.Heat(q, fit, 0, C), Stage.Heat(q, fit, 30, C));
        }

        [Test]
        public void N8_契合四项都空时等于底分()
        {
            var s = PlayFirstDay(out var gid, out _);
            // 绢不在西施的偏好里，首发角色没有朝代偏好与词条偏好
            Assert.AreEqual(C.balance.affection.fitBase, Stage.Fit(s, C, Find.Garment(s, gid), "xiShi"));
        }

        [Test]
        public void N9_赵飞燕首发对素纱计品种命中_花纱启用后不再计()
        {
            var row = C.Character("zhaoFeiYan");
            Assert.IsTrue(Stage.VarietyHit(C, row, "suSha"));
            var huaSha = C.Variety("huaSha");
            bool old = huaSha.launch;
            try
            {
                huaSha.launch = true;
                Assert.IsFalse(Stage.VarietyHit(C, row, "suSha"));
                Assert.IsTrue(Stage.VarietyHit(C, row, "huaSha"));
            }
            finally { huaSha.launch = old; }
        }

        [Test]
        public void N11_精良布出售被拒绝()
        {
            var s = NewGame();
            var b = Find.Bolt(s, NewGameFactory.OpeningBoltId);
            var fine = C.balance.quality.bands.Find(x => x.tier == QualityCalc.Fine);
            b.materialScore = fine.qMax; b.yarnScore = fine.qMax; b.weaveScore = fine.qMax;
            Assert.IsFalse(Market.SellBolt(s, C, b.id).ok);
            Assert.IsNotNull(Find.Bolt(s, b.id));
        }

        [Test]
        public void S2_缺cloth时读档能重算()
        {
            var s = NewGame();
            var back = SaveSerializer.FromJson(SaveSerializer.ToJson(s));
            Assert.IsNull(back.bolts[0].cloth);
            ClothSync.RecomputeAll(back, C);
            Assert.IsNotNull(back.bolts[0].cloth);
            var v = C.Variety(back.bolts[0].variety);
            Assert.That(back.bolts[0].cloth.density, Is.InRange(v.densityMin, v.densityMax));
        }

        [Test]
        public void S3_clothLocked存在时不被重算覆盖()
        {
            var s = NewGame();
            var locked = new ClothDescriptor { density = -1, bend = -1 };
            s.bolts[0].clothLocked = locked;
            s.bolts[0].cloth = locked;
            var back = SaveSerializer.FromJson(SaveSerializer.ToJson(s));
            ClothSync.RecomputeAll(back, C);
            Assert.AreEqual(-1, back.bolts[0].cloth.density);
            Assert.AreEqual(-1, back.bolts[0].clothLocked.density);
        }

        [Test]
        public void 三级前没有日常委托_三级后按哈希刷新且读档不重掷()
        {
            var s = NewGame();
            Quests.Refresh(s, C);
            Assert.IsFalse(s.quests.Exists(q => !q.tutorial), "3 级前教学不被随机委托打断");
            s.level = C.balance.questRules.dailyFromLevel;
            var copy = SaveSerializer.FromJson(SaveSerializer.ToJson(s));
            Quests.Refresh(s, C);
            Quests.Refresh(copy, C);
            Assert.AreEqual(C.balance.questRules.dailyCount, s.quests.FindAll(q => !q.tutorial).Count);
            Assert.AreEqual(SaveSerializer.ToJson(s), SaveSerializer.ToJson(copy), "同档同日刷新结果相同");
        }

        [Test]
        public void 春蚕能缫出细丝()
        {
            var s = NewGame();
            s.silkCoin = 100;
            Assert.IsTrue(Silk.Hatch(s, C).ok);
            Assert.IsFalse(Silk.Feed(s, C, false).ok, "相邻两步要隔一个工坊日");
            Day.End(s, C);
            Assert.IsTrue(Silk.Feed(s, C, false).ok);
            Day.End(s, C);
            Assert.IsTrue(Silk.Temper(s, C, true).ok);
            Day.End(s, C);
            var m = Silk.Mount(s, C, false);
            Assert.IsTrue(m.ok, m.error);
            var r = Silk.Reel(s, C, m.createdId, "fine", Steady(C.balance.season.reelBeats));
            Assert.IsTrue(r.ok, r.error);
            var y = Find.Yarn(s, r.createdId);
            Assert.AreEqual("fine", y.fineness);
            Assert.AreEqual(C.balance.day.reelYieldMeters, y.length, 1e-6);
            Assert.AreEqual(0, y.joints);
        }

        [Test]
        public void 织布按纱长出布并耗工时()
        {
            var s = NewGame();
            s.dayIndex = 1; // 普通工坊日
            var input = new Craft.WeaveInput { yarnId = NewGameFactory.OpeningYarnId, varietyId = "suSha", patternId = Craft.PatternPlain };
            for (int i = 0; i < C.balance.weave.segments; i++) input.segments.Add(Steady(C.balance.weave.beatsPerSegment));
            var r = Craft.Weave(s, C, input);
            Assert.IsTrue(r.ok, r.error);
            var b = Find.Bolt(s, r.createdId);
            Assert.AreEqual(C.opening.yarn.length * C.balance.day.clothPerYarnMeter, b.length, 1e-6);
            Assert.AreEqual(100, b.weaveScore, "全稳拍、素布");
            Assert.AreEqual(C.balance.day.HoursOf("weavePlain"), s.hoursUsed);
        }

        [Test]
        public void 季节_三级起轮转_换季鲜料转干_冬不收蚁()
        {
            var s = NewGame();
            var sc = C.balance.season;
            Day.End(s, C);
            Assert.AreEqual("spring", s.season, "3 级前恒为春");
            Assert.IsNull(s.seasonStartDay);
            s.level = sc.rotationFromLevel;
            Day.End(s, C);
            Assert.AreEqual(s.dayIndex, s.seasonStartDay, "达到等级后的下一个工坊日开始轮转");
            Assert.IsTrue(Market.BuyFreshDye(s, C, "indigo").ok, "春有幼靛（记作靛蓝）");
            int dryBefore = Find.Dye(s, "indigo").count;
            for (int i = 0; i < sc.daysPerSeason; i++) Day.End(s, C);
            Assert.AreEqual("summer", s.season);
            Assert.IsNull(Find.Dye(s, "indigo", true), "换季后鲜料不在");
            Assert.AreEqual(dryBefore + 1, Find.Dye(s, "indigo").count, "鲜料晒成同种干料");
            for (int i = 0; i < sc.daysPerSeason * 2; i++) Day.End(s, C);
            Assert.AreEqual("winter", s.season);
            Assert.IsFalse(Silk.Hatch(s, C).ok, "冬不收蚁");
        }
    }
}
