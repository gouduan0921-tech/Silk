using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using HuaShang.Game;
using HuaShang.Performance;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Save;
using HuaShang.Solve;

namespace HuaShang.Tests
{
    /// <summary>
    /// 发布门（docs/26 §1）在运行时的验收：第 4 条穿得上、第 6 条呈现模式不改仓库、
    /// 第 8 条低布料质量、第 9 条静音音乐后仍有操作声。第 1、2、3、5、7 条见 WorkshopFlowTests、ClothProofTests 与 EditMode。
    /// </summary>
    public class ReleaseGateTests : ChainSceneTestBase
    {
        static List<Beat> All(Beat b, int n)
        {
            var l = new List<Beat>();
            for (int i = 0; i < n; i++) l.Add(b);
            return l;
        }

        /// <summary>用玩法命令直接做一件靛蓝襦裙；上襦按 upperBeat 落针。</summary>
        string MakeGarment(Beat upperBeat)
        {
            var c = G.Config;
            string bolt = NewGameFactory.OpeningBoltId;
            Assert.IsTrue(G.Run((s, cc) => Craft.ConfirmBolt(s, cc, bolt)).ok);
            Assert.IsTrue(G.Run((s, cc) => Craft.Dye(s, cc, new Craft.DyeInput
            {
                boltId = bolt, dyeId = "indigo", concentration = DyeCalc.Medium, temperature = Craft.TempWarm,
                liftSeconds = cc.balance.dye.LiftCenterOf(DyeCalc.Medium), stirSteady = 1, stirOff = 0, stirTotal = 1,
            })).ok);
            var u = G.Run((s, cc) => Craft.Cut(s, cc, new Craft.CutInput { boltId = bolt, slot = "upper" }));
            var k = G.Run((s, cc) => Craft.Cut(s, cc, new Craft.CutInput { boltId = bolt, slot = "skirt" }));
            int needles = c.balance.weave.sewNeedlesPerPart;
            Assert.IsTrue(G.Run((s, cc) => Craft.Sew(s, cc, u.createdId, All(upperBeat, needles))).ok);
            Assert.IsTrue(G.Run((s, cc) => Craft.Sew(s, cc, k.createdId, All(Beat.Steady, needles))).ok);
            var g = G.Run((s, cc) => Craft.Assemble(s, cc, new Craft.AssembleInput { pieceIds = new List<string> { u.createdId, k.createdId } }));
            Assert.IsTrue(g.ok, g.error);
            return g.createdId;
        }

        IEnumerator EnterStage()
        {
            W.Approach(W.stations.Find(s => s.stationId == "station_stage"), true);
            yield return Frames(60);
            Assert.AreEqual("上场门", P.title);
        }

        IEnumerator SelectOption(string label, string choice)
        {
            var o = P.options.Find(x => x.label == label);
            Assert.IsNotNull(o, "上场门没有选项：" + label);
            int i = o.choices.IndexOf(choice);
            Assert.GreaterOrEqual(i, 0, label + " 里没有 " + choice);
            o.selected = i;
            o.onSelect?.Invoke(i);
            yield return Frames(2);
        }

        PerformanceDirector Director => Object.FindFirstObjectByType<PerformanceDirector>();

        [UnityTest]
        public IEnumerator 门4_缝制分压到阈值以下_衣服仍穿得上并演完0档()
        {
            string gid = MakeGarment(Beat.Chaos);
            var upper = Find.Garment(S, gid).parts.Find(p => p.slot == "upper");
            Assert.Less(upper.sewScore.Value, G.Config.balance.penalties.sewLowThreshold);
            yield return EnterStage();
            yield return Primary(1);
            var pd = Director;
            Assert.IsTrue(pd.Playing);

            var bolt = Find.Bolt(S, upper.boltId);
            var group = G.Config.StretchGroup(G.Config.Variety(bolt.variety).stretchGroup);
            var parts = pd.visual.parts;
            var upperPart = parts.Find(p => p.name.Contains("upper"));
            Assert.IsNotNull(upperPart, "上襦要穿在身上");
            Assert.AreEqual(group.distanceMin, upperPart.descriptor.stretch, 1e-9, "拉伸取该行低端");

            var verts = new List<Vector3>();
            float hipsY = pd.performer.hips.position.y;
            int frames = Mathf.CeilToInt((float)pd.director.duration * 60f) + 30;
            for (int f = 0; f < frames; f++)
            {
                yield return null;
                if (f % 15 != 0) continue;
                foreach (var part in parts)
                {
                    ClothSampler.WorldVertices(part, verts);
                    foreach (var v in verts)
                    {
                        Assert.IsFalse(float.IsNaN(v.x + v.y + v.z) || float.IsInfinity(v.x + v.y + v.z), "炸布");
                        Assert.Less(Vector3.Distance(v, pd.performer.transform.position + Vector3.up), 2.5f, "布飞离身体");
                    }
                }
                ClothSampler.WorldVertices(upperPart, verts);
                float top = float.MinValue;
                foreach (var v in verts) top = Mathf.Max(top, v.y);
                Assert.Greater(top, hipsY, "上襦领口仍挂在肩上，没有滑落到腰下");
            }
            Assert.IsFalse(pd.Playing, "0 档演完");
            Assert.AreEqual(1, S.performances.Count);
        }

        [UnityTest]
        public IEnumerator 门6_视觉增强演出不改仓库的染层与cloth()
        {
            MakeGarment(Beat.Steady);
            string before = SaveSerializer.ToJson(new SaveRoot { schema = S.schema, bolts = S.bolts });
            yield return EnterStage();
            yield return SelectOption("呈现", "视觉增强");
            yield return Primary(1);
            var pd = Director;
            yield return Seconds((float)pd.director.duration + 1f);
            Assert.AreEqual(1, S.performances.Count);
            Assert.AreEqual(ClothDescribe.ModeEnhanced, S.performances[0].presentMode, "这一场确实按视觉增强演出");
            string after = SaveSerializer.ToJson(new SaveRoot { schema = S.schema, bolts = S.bolts });
            Assert.AreEqual(before, after, "呈现模式只改显示拷贝");
        }

        [UnityTest]
        public IEnumerator 门8_低布料质量下颜色正确且0档能看完()
        {
            Settings.Current.clothQuality = (int)ClothQuality.Low;
            Settings.Apply();
            string gid = MakeGarment(Beat.Steady);
            yield return EnterStage();
            yield return Primary(1);
            var pd = Director;
            yield return Frames(30);
            var c = G.Config;
            foreach (var part in pd.visual.parts)
            {
                Assert.IsFalse(part.IsSimulated, part.name + "：低档不实时解算（docs/03 §3）");
                var g = Find.Garment(S, gid);
                var gp = g.parts.Find(x => part.name.Contains(x.slot));
                var bolt = Find.Bolt(S, gp.boltId);
                var want = ClothLook.DyedColor(bolt.dyeLayers, c.balance.dye);
                var have = ClothLook.ReadColor(part.material);
                Assert.AreEqual(want.r, have.r, 1e-4, part.name + " 颜色 r");
                Assert.AreEqual(want.g, have.g, 1e-4, part.name + " 颜色 g");
                Assert.AreEqual(want.b, have.b, 1e-4, part.name + " 颜色 b");
                Assert.AreEqual(ClothLook.Alpha(part.descriptor, c.clothFixed), have.a, 1e-4, part.name + " 透明度");
            }
            yield return Seconds((float)pd.director.duration + 1f);
            Assert.IsFalse(pd.Playing, "低档下 0 档能看完");
            Assert.AreEqual(1, S.performances.Count);
        }

        [UnityTest]
        public IEnumerator 门9_静音音乐后织机仍有操作声()
        {
            Settings.Current.musicVolume = 0f;
            Settings.Apply();
            Assert.AreEqual(0f, Sfx.I.music.volume);
            W.Approach(W.stations.Find(s => s.stationId == "station_loom"), true);
            yield return Frames(30);
            yield return Secondary("新布", 5);      // 开局先是确认绢 A；改织一匹新布
            yield return Pick(NewGameFactory.OpeningYarnId);
            yield return Primary(1);                // 开织
            int before = Sfx.PlayedCount;
            yield return Beats(4);
            Assert.GreaterOrEqual(Sfx.PlayedCount - before, 4, "每一梭都有操作声");
            Assert.IsTrue(Sfx.I.OperationAudible, "操作声音量不跟音乐一起关");
            Assert.IsFalse(AudioListener.pause);
            Assert.Greater(AudioListener.volume, 0f);
        }
    }
}
