using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using HuaShang.Data;
using HuaShang.DocImport;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Tests
{
    /// <summary>P0：表能加载、schema 1 能读写（S1）、守门检查。</summary>
    public class ConfigAndSaveTests
    {
        static ConfigSnapshot C => Fixture.Config;

        [Test]
        public void 手册能读出且通过docs05第9节校验()
        {
            var errors = ConfigValidator.Validate(C);
            Assert.IsEmpty(errors, string.Join("\n", errors));
        }

        [Test]
        public void 关闭的行仍在表中()
        {
            Assert.IsTrue(C.varieties.Exists(v => !v.launch), "docs/04 §8 的非首发品种应保留");
            // 工艺章后八种染料都已开放；染料表的行数不随开放而变
            Assert.AreEqual(8, C.dyes.Count, "docs/05 §3 的染料行都在表里");
            Assert.IsTrue(C.characters.Exists(ch => !ch.enabled), "非首发角色应保留且关闭");
            Assert.IsTrue(C.stages.Exists(s => !s.launch), "关闭的舞台应保留");
            Assert.AreEqual(C.balance.economy.levels.Count, C.unlocks.Count, "等级表每一级都在");
            Assert.IsTrue(C.activities.TrueForAll(a => !a.enabled), "活动默认关闭");
        }

        [Test]
        public void 首发角色与舞台()
        {
            var enabled = C.characters.FindAll(ch => ch.enabled);
            CollectionAssert.AreEquivalent(new[] { "xiShi", "wangZhaoJun", "zhaoFeiYan", "liQingZhao", "yangGuiFei" }, enabled.ConvertAll(ch => ch.id), "首发三位，工艺章加李清照、杨贵妃（docs/16 §2）");
            Assert.IsTrue(C.stages.Find(s => s.id == "stage_classic").launch);
        }

        [Test]
        public void 开局物品()
        {
            var o = C.opening;
            Assert.AreEqual("juan", o.bolt.variety);
            Assert.IsFalse(o.bolt.dyed);
            Assert.IsTrue(o.trayEmpty);
            Assert.AreEqual("spring", o.season);
            CollectionAssert.AreEquivalent(new[] { "indigo", "madder" }, o.dyes.ConvertAll(d => d.dyeId));
            Assert.IsTrue(o.dyes.TrueForAll(d => d.state == "dry"));
        }

        [Test]
        public void 解算固定项来自docs03()
        {
            // 值本身只在 docs/03 §2；这里只确认三项都读到了且在 MagicaCloth 2 的合法范围内。
            var f = C.clothFixed;
            Assert.GreaterOrEqual(f.gravityFalloff, 0);
            Assert.That(f.triangleBendingStiffness, Is.InRange(0.0, 1.0));
            Assert.Greater(f.triangleBendingStiffness, 0);
            Assert.Greater(f.radius, 0);
        }

        [Test]
        public void S1_schema1能读写()
        {
            var root = NewGameFactory.Create(C, "test-save");
            string json = SaveSerializer.ToJson(root);
            var back = SaveSerializer.FromJson(json);

            Assert.AreEqual(1, back.schema);
            Assert.AreEqual(root.silkCoin, back.silkCoin);
            Assert.AreEqual(root.level, back.level);
            Assert.AreEqual(root.bolts[0].id, back.bolts[0].id);
            Assert.AreEqual(root.bolts[0].length, back.bolts[0].length);
            Assert.AreEqual(root.yarns[0].length, back.yarns[0].length);
            Assert.AreEqual(C.opening.yarn.materialScore, back.yarns[0].materialScore);
            Assert.AreEqual(root.dyes.Count, back.dyes.Count);
            Assert.AreEqual(TrayStage.Empty, back.tray.stage);
            Assert.IsEmpty(back.garments, "没有开局成衣");
            Assert.IsEmpty(back.pieces);
            Assert.AreEqual(json, SaveSerializer.ToJson(back), "再写一次应完全相同");
        }

        [Test]
        public void S1_读档不改布匹id与染层()
        {
            var root = NewGameFactory.Create(C, "test-save");
            root.bolts[0].dyeLayers.Add(new DyeLayer { dyeId = "indigo", strength = 0.4, uneven = 0.1, maskId = "mask_1" });
            var back = SaveSerializer.FromJson(SaveSerializer.ToJson(root));
            Assert.AreEqual("bolt_A", back.bolts[0].id);
            Assert.AreEqual(1, back.bolts[0].dyeLayers.Count);
            Assert.AreEqual("indigo", back.bolts[0].dyeLayers[0].dyeId);
            Assert.AreEqual(0.4, back.bolts[0].dyeLayers[0].strength, 1e-12);
        }

        [Test]
        public void S1_拒绝其他schema()
        {
            var root = NewGameFactory.Create(C, "test-save");
            string json = SaveSerializer.ToJson(root).Replace("\"schema\": 1", "\"schema\": 2");
            Assert.Throws<SaveSchemaException>(() => SaveSerializer.FromJson(json));
        }

        [Test]
        public void S1_写盘与读盘()
        {
            string path = Path.Combine(Path.GetTempPath(), "huashang_s1_" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                var root = NewGameFactory.Create(C, "test-save");
                SaveSerializer.WriteFile(path, root);
                SaveSerializer.WriteFile(path, root); // 覆盖写
                var back = SaveSerializer.ReadFile(path);
                Assert.AreEqual(root.rngSalt, back.rngSalt);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void 守门_玩法代码不用UnityEngine随机()
        {
            string scripts = Path.Combine(Fixture.ProjectRoot, "Assets", "HuaShang", "Scripts");
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Replace('\\', '/').Contains("/Tests/")) continue;
                string text = File.ReadAllText(file);
                Assert.IsFalse(Regex.IsMatch(text, "UnityEngine\\.Random|\\bRandom\\.(Range|value|InitState)"),
                               file + " 使用了 UnityEngine.Random；随机只能用 StableHash（docs/19 §4）");
            }
        }

        [Test]
        public void 数据资产与手册一致()
        {
            var db = AssetDatabase.LoadAssetAtPath<ConfigDatabase>("Assets/HuaShang/Data/ConfigDatabase.asset");
            if (db == null) Assert.Ignore("尚未导入，请先运行「HuaShang/从 docs 导入配置表」。");
            var fromAsset = db.ToSnapshot();
            var fromDocs = DocsConfigReader.ReadAll(Fixture.ProjectRoot);
            fromAsset.notes.Clear();
            fromDocs.notes.Clear();
            Assert.AreEqual(JsonUtility.ToJson(fromDocs), JsonUtility.ToJson(fromAsset),
                            "Data 下的资产与 docs 不一致，请重新导入。");
        }
    }
}
