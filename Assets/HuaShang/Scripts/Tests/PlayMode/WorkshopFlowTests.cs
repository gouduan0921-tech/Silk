using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HuaShang.Game;
using HuaShang.Performance;
using HuaShang.Play;
using HuaShang.Save;
using HuaShang.Stations;
using HuaShang.UI;

namespace HuaShang.Tests
{
    /// <summary>
    /// 运行时走查（docs/26 §3 的首日流程与演出项）：在 HS_Chain 场景里按木牌按钮走一遍，
    /// 判定只读存档与界面模型，数字都取自手册导入的配置。用单独的测试存档，不碰玩家存档。
    /// </summary>
    public class WorkshopFlowTests
    {
        const string TestSave = "huashang_playmode_test.json";

        GameSession G => GameSession.I;
        SaveRoot S => G.Save;
        WorkshopController W => Object.FindFirstObjectByType<WorkshopController>();
        PlaqueModel P => Hud.I.CurrentPlaque;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            GameSession.SaveFileOverride = TestSave;
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("HS_Chain");
            yield return Frames(5);
            G.StartNewGame();
            yield return Frames(3);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.captureDeltaTime = 0;
            Time.timeScale = 1f;
            GameSession.SaveFileOverride = null;
            yield return null;
        }

        static IEnumerator Frames(int n) { for (int i = 0; i < n; i++) yield return null; }

        static IEnumerator Seconds(float s) => Frames(Mathf.CeilToInt(s * 60f));

        IEnumerator Primary(int frames = 20)
        {
            Assert.IsNotNull(P.onPrimary, "木牌没有主按钮：" + P.title);
            Assert.IsTrue(P.primaryEnabled, "主按钮是灰的：" + P.primaryLabel + "（" + P.body + "）");
            P.onPrimary();
            yield return Frames(frames);
        }

        IEnumerator Secondary(string label, int frames = 20)
        {
            var s = P.secondary.Find(x => x.Key.Contains(label));
            Assert.IsNotNull(s.Value, "木牌没有次按钮：" + label);
            s.Value();
            yield return Frames(frames);
        }

        IEnumerator Pick(string id)
        {
            var f = typeof(Hud).GetField("rackItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var items = (List<RackItem>)f.GetValue(Hud.I);
            var it = items.Find(x => x.id == id);
            Assert.IsNotNull(it, "侧架上没有 " + id);
            it.onClick();
            yield return Frames(2);
        }

        /// <summary>每拍目标点上按空格（稳拍）。</summary>
        IEnumerator Beats(int count)
        {
            float interval = (float)G.Config.balance.weave.beatInterval;
            float t0 = Time.time;
            int pressed = 0;
            int guard = Mathf.CeilToInt((count + 2) * interval * 60f) + 10;
            for (int i = 0; i < guard && pressed < count; i++)
            {
                yield return null;
                if (Time.time - t0 >= interval * (pressed + 1)) { W.current.BeatKey(); pressed++; }
            }
            yield return Frames(2);
        }

        /// <summary>教学路线走到人台收成襦裙；顺带裁下披帛留着不缝（docs/10 §2 首日结束状态）。</summary>
        IEnumerator Day1ToGarment()
        {
            var w = W;
            w.Approach(w.stations.Find(s => s.stationId == "station_loom"), true);
            yield return Frames(5);
            yield return Primary();                 // 确认开局绢
            yield return Primary(5);                // 走去染缸
            yield return Primary(40);               // 进入
            yield return Pick("indigo");
            yield return Primary(1);                // 浸布
            var dye = G.Config.balance.dye;
            float center = (float)dye.LiftCenterOf(HuaShang.Rules.Calc.DyeCalc.Medium);
            float interval = (float)G.Config.balance.weave.beatInterval;
            int beats = Mathf.FloorToInt(center / interval);
            yield return Beats(beats);
            yield return Seconds(center - interval * beats);
            yield return Primary();                 // 起布
            yield return Primary(5);                // 走去裁桌
            yield return Primary(40);
            yield return Primary(10);               // 上襦
            yield return Primary(10);               // 裙
            yield return Primary(10);               // 披帛（自动跳到下一部件）
            yield return Secondary("针线", 5);
            yield return Primary(40);
            int needles = G.Config.balance.weave.sewNeedlesPerPart;
            yield return Primary(1); yield return Beats(needles); yield return Primary(5);
            yield return Pick(S.pieces.Find(p => p.slot == "skirt").id);
            yield return Primary(1); yield return Beats(needles);
            yield return Primary(5);                // 接受这一缝
            if (P.primaryLabel == "走近人台") yield return Primary(5);
            else yield return Secondary("人台", 5); // 披帛可以留着不缝

            yield return Primary(90);               // 进入人台
            yield return Primary(30);               // 收成襦裙
        }

        [UnityTest]
        public IEnumerator 首日_三条教学在教学日工时内走完()
        {
            yield return Day1ToGarment();
            Assert.AreEqual(1, S.garments.Count, "人台应收成一件襦裙");
            var g = S.garments[0];
            Assert.IsNotNull(g.tier, "成衣要有档位");

            var stage = W.stations.Find(s => s.stationId == "station_stage");
            W.Approach(stage, true);
            yield return Frames(60);
            Assert.AreEqual("上场门", P.title);
            yield return Primary(1);                // 开演
            var pd = Object.FindFirstObjectByType<PerformanceDirector>();
            Assert.IsTrue(pd.Playing);
            yield return Seconds((float)pd.director.duration + 1f);
            Assert.IsFalse(pd.Playing, "时间轴放完应定格");
            Assert.AreEqual(1, S.performances.Count);

            yield return Primary(5);                // 走去展柜
            yield return Primary(60);
            yield return Pick(g.id);
            yield return Primary(30);               // 放入第 1 展位

            foreach (var id in new[] { Craft.Tutorial1, Craft.Tutorial2, Craft.Tutorial3 })
                Assert.IsTrue(Find.Quest(S, id).done, id + " 应完成");
            var day = G.Config.balance.day;
            Assert.LessOrEqual(S.hoursUsed, day.HoursLimit(0), "首日工时不能超出教学日上限");
            Assert.Greater(S.hoursUsed, day.hoursPerDay, "教学路线本身要用到教学日多出的工时（构架文档 G1）");
            Assert.AreEqual(1, Find.Dye(S, "indigo").count, "首日结束剩干靛蓝 1 份");
            Assert.AreEqual(1, Find.Dye(S, "madder").count, "干茜草未动");
            Assert.IsTrue(S.pieces.Exists(p => p.slot == "drape" && !p.sewScore.HasValue), "留一片未缝披帛");

            // 离开工位即写盘
            W.GoOverview();
            yield return Frames(2);
            var onDisk = SaveSerializer.ReadFile(G.SavePath);
            Assert.AreEqual(S.garments.Count, onDisk.garments.Count);
            Assert.AreEqual(S.hoursUsed, onDisk.hoursUsed);
        }

        [UnityTest]
        public IEnumerator 演出暂停时时间轴与布一起停_演完才记录()
        {
            yield return Day1ToGarment();
            W.Approach(W.stations.Find(s => s.stationId == "station_stage"), true);
            yield return Frames(60);
            yield return Primary(1);
            var pd = Object.FindFirstObjectByType<PerformanceDirector>();
            yield return Seconds(3f);

            pd.TogglePause();
            yield return Frames(2);
            double t = pd.director.time;
            var part = pd.visual.GetComponentInChildren<HuaShang.Solve.ClothPart>();
            var mesh = part.GetComponent<MeshFilter>().sharedMesh;
            var before = mesh.vertices;
            yield return Seconds(1.5f);
            Assert.IsTrue(pd.Paused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(t, pd.director.time, 1e-6, "暂停时时间轴不走");
            var after = mesh.vertices;
            float moved = 0;
            for (int i = 0; i < before.Length; i++) moved = Mathf.Max(moved, (after[i] - before[i]).magnitude);
            Assert.Less(moved, 1e-5f, "暂停时布不动");
            Assert.AreEqual(0, S.performances.Count, "演出没放完不记录");

            pd.TogglePause();
            yield return Seconds((float)(pd.director.duration - t) + 1f);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(1, S.performances.Count);
        }

        [UnityTest]
        public IEnumerator 上场门只列已解锁且有时间轴的角色()
        {
            yield return Day1ToGarment();
            W.Approach(W.stations.Find(s => s.stationId == "station_stage"), true);
            yield return Frames(60);
            var who = P.options.Find(o => o.label == "上场");
            Assert.IsNotNull(who);
            CollectionAssert.AreEqual(new[] { "西施" }, who.choices, "1 级只有西施（docs/16 §2）");
            var tiers = P.options.Find(o => o.label == "档位");
            CollectionAssert.AreEqual(new[] { "0 档" }, tiers.choices, "30 档要好感到门槛才出现（docs/04 §6）");
        }
    }
}
