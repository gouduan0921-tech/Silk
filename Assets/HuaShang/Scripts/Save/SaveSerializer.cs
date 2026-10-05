using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using HuaShang.Rules.Config;

namespace HuaShang.Save
{
    public class SaveSchemaException : Exception
    {
        public SaveSchemaException(string message) : base(message) { }
    }

    /// <summary>schema 1 的读写（S1）。不在这里重算 cloth：读档后由解算模块按 docs/03 处理（S2、S3）。</summary>
    public static class SaveSerializer
    {
        public const int Schema = 1;

        static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            FloatFormatHandling = FloatFormatHandling.String,
        };

        public static string ToJson(SaveRoot root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (root.schema != Schema) throw new SaveSchemaException("只能写 schema " + Schema + "，当前为 " + root.schema);
            return JsonConvert.SerializeObject(root, Settings);
        }

        public static SaveRoot FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new SaveSchemaException("存档为空");
            var root = JsonConvert.DeserializeObject<SaveRoot>(json, Settings);
            if (root == null) throw new SaveSchemaException("存档无法解析");
            if (root.schema != Schema)
                throw new SaveSchemaException("存档 schema 为 " + root.schema + "，本版本只读 schema " + Schema + "，不迁移（docs/21 §1）");
            return root;
        }

        /// <summary>先写临时文件再替换，避免离开工位写盘时中断留下半个文件。</summary>
        public static void WriteFile(string path, SaveRoot root)
        {
            string json = ToJson(root);
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            if (File.Exists(path)) File.Replace(tmp, path, null);
            else File.Move(tmp, path);
        }

        public static SaveRoot ReadFile(string path) => FromJson(File.ReadAllText(path, Encoding.UTF8));
    }

    /// <summary>新档：根与开局物品全部来自 docs/04 §1（经配置表）。</summary>
    public static class NewGameFactory
    {
        /// <summary>开局物品的运行时 id。docs/04 §1 称作「布 A」「丝 B」。</summary>
        public const string OpeningBoltId = "bolt_A";
        public const string OpeningYarnId = "yarn_B";
        public const string DefaultPresentMode = "standard";

        public static SaveRoot Create(ConfigSnapshot c, string saveId)
        {
            if (c == null) throw new ArgumentNullException(nameof(c));
            if (string.IsNullOrEmpty(saveId)) throw new ArgumentException("需要档 id", nameof(saveId));
            var o = c.opening;
            var launchDynasty = c.balance.quality.dynasties.Find(d => d.launch && d.parsed);
            if (launchDynasty == null) throw new InvalidOperationException("docs/04 §4 没有首发朝代偏移");

            var root = new SaveRoot
            {
                schema = SaveSerializer.Schema,
                dayIndex = 0,
                season = o.season,
                silkCoin = o.silkCoin,
                xp = o.xp,
                level = o.level,
                presentMode = DefaultPresentMode,
                rngSalt = saveId,
            };

            root.yarns.Add(new Yarn
            {
                id = OpeningYarnId,
                fiber = o.yarn.fiber,
                fineness = o.yarn.fineness,
                processScore = o.yarn.processScore,
                materialScore = o.yarn.materialScore,
                joints = o.yarn.joints,
                length = o.yarn.length,
            });

            var bolt = new Bolt
            {
                id = OpeningBoltId,
                variety = o.bolt.variety,
                dynastyStyle = launchDynasty.dynasty,
                yarnId = null,
                materialScore = o.bolt.materialScore,
                yarnScore = o.bolt.yarnScore,
                length = o.bolt.length,
                width = o.bolt.width,
                weaveScore = o.bolt.weaveScore,
                patternId = null,
                finish = null,
                edgeDamage = 0,
            };
            if (o.bolt.dyed) throw new InvalidOperationException("docs/04 §1 的开局布 A 应为未染");
            root.bolts.Add(bolt);

            foreach (var d in o.dyes)
                root.dyes.Add(new DyeStock { dyeId = d.dyeId, count = d.count, state = d.state });

            if (!o.trayEmpty) throw new InvalidOperationException("docs/04 §1 的蚕箔应为空");
            root.tray = new Tray { stage = TrayStage.Empty };

            var xiShi = c.characters.Find(ch => ch.id == "xiShi");
            if (xiShi != null && xiShi.enabled)
                root.characters.Add(new CharacterState { id = xiShi.id, affection = o.xiShiAffection });

            foreach (var q in c.quests)
                if (q.tutorial) root.quests.Add(new QuestState { id = q.id, done = false, tutorial = true });

            return root;
        }
    }
}
