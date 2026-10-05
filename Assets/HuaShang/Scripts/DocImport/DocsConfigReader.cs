using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using HuaShang.Rules.Config;

namespace HuaShang.DocImport
{
    /// <summary>
    /// 从 docs/03、04、05、07、10、16 读出全部配置表。数字只从 docs/04（和 docs/03 §2 的固定项）读，
    /// 本文件不含任何玩法数值；这里只有「中文名 → 手册 id」的对照，以及手册未给出的暂定 id。
    /// 读不出时抛 DocParseException，写明文件与小节。
    /// </summary>
    public static class DocsConfigReader
    {
        public const string Doc03 = "03_布料仿真技术实现方案.md";
        public const string Doc04 = "04_数值平衡初版.md";
        public const string Doc05 = "05_配置表结构.md";
        public const string Doc07 = "07_织染裁缝工序.md";
        public const string Doc09 = "09_博物馆策展.md";
        public const string Doc10 = "10_升级与成长.md";
        public const string Doc16 = "16_美人与好感.md";

        // ---- 中文名与 id 的对照 ----

        /// <summary>docs/04 §8 品种。前四个 id 见 docs/05 §2；其余 docs/05 未给出，暂按同一拼写方式，待确认。</summary>
        static readonly Dictionary<string, string> VarietyIds = new Dictionary<string, string>
        {
            { "素纱", "suSha" }, { "绢", "juan" }, { "绸", "chou" }, { "苎麻", "zhuMa" },
            { "花纱", "huaSha" }, { "素罗", "suLuo" }, { "花罗", "huaLuo" }, { "绫", "ling" },
            { "素缎", "suDuan" }, { "花缎", "huaDuan" }, { "普通锦", "puTongJin" }, { "蜀锦", "shuJin" },
            { "宋锦", "songJin" }, { "云锦", "yunJin" }, { "妆花缎", "zhuangHuaDuan" }, { "缂丝", "keSi" },
            { "改机", "gaiJi" }, { "麻", "ma" }, { "葛", "ge" },
        };
        static readonly HashSet<string> HandbookVarietyIds = new HashSet<string> { "suSha", "juan", "chou", "zhuMa" };

        /// <summary>docs/04 §8 分组表的组 → docs/05 §2 的 group 值。</summary>
        static readonly Dictionary<string, string> GroupIds = new Dictionary<string, string>
        {
            { "纱罗", "gauze" }, { "绢绸绫缎、改机", "plain" }, { "锦、云锦、妆花、缂丝", "brocade" }, { "麻葛", "bast" },
        };

        /// <summary>docs/04 §8 分组表中各品种归属。分组表用简称列出成员，此处逐一展开。</summary>
        static readonly Dictionary<string, string> VarietyGroup = new Dictionary<string, string>
        {
            { "素纱", "gauze" }, { "花纱", "gauze" }, { "素罗", "gauze" }, { "花罗", "gauze" },
            { "绢", "plain" }, { "绸", "plain" }, { "绫", "plain" }, { "素缎", "plain" }, { "花缎", "plain" }, { "改机", "plain" },
            { "普通锦", "brocade" }, { "蜀锦", "brocade" }, { "宋锦", "brocade" }, { "云锦", "brocade" },
            { "妆花缎", "brocade" }, { "缂丝", "brocade" },
            { "麻", "bast" }, { "苎麻", "bast" }, { "葛", "bast" },
        };

        /// <summary>docs/05 §3 的 id 与中文名。</summary>
        static readonly Dictionary<string, string> DyeNames = new Dictionary<string, string>
        {
            { "indigo", "靛蓝" }, { "madder", "茜草" }, { "gardenia", "栀子" }, { "safflower", "红花" },
            { "pagoda", "槐米" }, { "sappan", "苏木" }, { "acorn", "皂斗" }, { "vitriol", "绿矾" },
        };
        /// <summary>docs/05 §3 的 role。绿矾在 docs/04 §5 是处理而非色料。</summary>
        static readonly HashSet<string> MordantDyes = new HashSet<string> { "vitriol" };

        /// <summary>docs/16 §3 的表只列 id，中文名在同节正文里；按此对照，并在读取时核对正文确有其名。</summary>
        static readonly Dictionary<string, string> ClosedCharacterNames = new Dictionary<string, string>
        {
            { "yangGuiFei", "杨贵妃" }, { "diaoChan", "貂蝉" }, { "liShiShi", "李师师" }, { "liQingZhao", "李清照" },
            { "chenYuanYuan", "陈圆圆" }, { "yuJi", "虞姬" }, { "hongFu", "红拂女" }, { "liuRuShi", "柳如是" },
        };

        static readonly Dictionary<string, string> Templates = new Dictionary<string, string>
        {
            { "空灵", "airy" }, { "清冷", "cold" }, { "灵动", "lively" }, { "华贵", "rich" }, { "强势", "strong" },
        };
        static readonly Dictionary<string, string> Dynasties = new Dictionary<string, string>
        {
            { "汉", "han" }, { "唐", "tang" }, { "宋", "song" }, { "明", "ming" },
        };
        static readonly Dictionary<string, string> Seasons = new Dictionary<string, string>
        {
            { "春", "spring" }, { "夏", "summer" }, { "秋", "autumn" }, { "冬", "winter" },
        };
        static readonly Dictionary<string, string> Fineness = new Dictionary<string, string>
        {
            { "细", "fine" }, { "中", "medium" }, { "粗", "coarse" },
        };
        /// <summary>docs/21 的 fiber 值。手册未给 id，暂定。</summary>
        static readonly Dictionary<string, string> Fibers = new Dictionary<string, string>
        {
            { "家蚕", "jiaCan" },
        };
        static readonly Dictionary<string, string> Tiers = new Dictionary<string, string>
        {
            { "普通", "common" }, { "精良", "fine" }, { "传世", "legendary" },
        };
        static readonly Dictionary<string, string> PresentModes = new Dictionary<string, string>
        {
            { "历史还原", "history" }, { "标准", "standard" }, { "视觉增强", "enhanced" },
        };
        static readonly Dictionary<string, string> HourKeys = new Dictionary<string, string>
        {
            { "喂蚕或控温", "feed" }, { "缫丝一束", "reel" }, { "平纹一匹", "weavePlain" },
            { "斜纹或素缎一匹", "weaveTwillSatin" }, { "提花、锦、缂丝一匹", "weaveJacquard" },
            { "浸染一匹", "dyeBath" }, { "扎染或夹缬", "dyeResist" },
            { "裁一个部件", "cutPart" }, { "缝一个部件", "sewPart" },
        };
        static readonly Dictionary<string, string> ModKeys = new Dictionary<string, string>
        {
            { "每层植物染", "plantLayer" }, { "浓档额外", "strongExtra" }, { "第 2 层起", "fromSecondLayer" },
            { "绿矾", "vitriol" }, { "砑光", "calender" }, { "金线掩膜内", "goldMask" },
        };
        static readonly Dictionary<string, string> SlotNames = new Dictionary<string, string>
        {
            { "上襦", "upper" }, { "裙", "skirt" }, { "披帛", "drape" }, { "衬里", "inner" },
            { "袍", "robe" }, { "大袖", "robe" }, { "褙子", "wrap" }, { "袄", "upper" },
        };
        static readonly Dictionary<string, string> PatternNames = new Dictionary<string, string>
        {
            { "襦裙", "ruQun" }, { "直裾", "zhiJu" }, { "大袖衫", "daXiuShan" }, { "褙子", "beiZi" }, { "袄裙", "aoQun" },
        };

        /// <summary>从工程根目录（含 docs/ 的目录）读出全部配置。</summary>
        public static ConfigSnapshot ReadAll(string projectRoot)
        {
            string docs = Path.Combine(projectRoot, "docs");
            var d03 = new MarkdownDoc(Path.Combine(docs, Doc03));
            var d04 = new MarkdownDoc(Path.Combine(docs, Doc04));
            var d05 = new MarkdownDoc(Path.Combine(docs, Doc05));
            var d07 = new MarkdownDoc(Path.Combine(docs, Doc07));
            var d10 = new MarkdownDoc(Path.Combine(docs, Doc10));
            var d16 = new MarkdownDoc(Path.Combine(docs, Doc16));

            var c = new ConfigSnapshot();
            ReadVarieties(d04.SectionOf(8), c);
            ReadDyes(d05.SectionOf(3), d04.SectionOf(3), c);
            c.balance = ReadBalance(d04);
            var d09 = new MarkdownDoc(Path.Combine(docs, Doc09));
            c.balance.exhibit.launchSlots = (int)d09.SectionOf(3).Number("柜内\\s*(\\d+)\\s*个展位");
            var q3 = d10.SectionOf(3);
            var qm = q3.MatchAll("(\\d+)\\s*级起每天刷新\\s*(\\d+)\\s*个，同时最多持有\\s*(\\d+)\\s*个，期限\\s*(\\d+)\\s*天");
            c.balance.questRules.dailyFromLevel = int.Parse(qm.Groups[1].Value);
            c.balance.questRules.dailyCount = int.Parse(qm.Groups[2].Value);
            c.balance.questRules.maxHeld = int.Parse(qm.Groups[3].Value);
            c.balance.questRules.days = int.Parse(qm.Groups[4].Value);
            var s8 = d04.SectionOf(8);
            c.balance.weave.coarseGauzeCap = s8.Number("粗丝织纱，织造分上限\\s*" + Num.Unsigned);
            c.balance.weave.legendaryGauzeMaxJoints = (int)s8.Number("接头\\s*≤\\s*(\\d+)");
            ReadPattern(d07.SectionOf(3), d04.SectionOf(2), c);
            ReadCharacters(d16, c);
            foreach (var r in d04.SectionOf(9).Table("时间轴"))
            {
                var m = Regex.Match(r[0], "^(\\S+?)\\s*(\\d+)\\s*档$");
                var ch = m.Success ? c.characters.Find(x => x.name == m.Groups[1].Value) : null;
                if (ch == null) throw new DocParseException("docs/04 §9 时间轴「" + r[0] + "」对不上角色");
                c.balance.windLight.timelines.Add(new TimelineLength
                {
                    characterId = ch.id,
                    tier = int.Parse(m.Groups[2].Value),
                    setup = Num.Parse(r[1], "docs/04 §9"),
                    develop = Num.Parse(r[2], "docs/04 §9"),
                    climax = Num.Parse(r[3], "docs/04 §9"),
                    coda = Num.Parse(r[4], "docs/04 §9"),
                });
            }
            ReadStages(d05.SectionOf(6), c);
            ReadQuests(d10.SectionOf(2), c);
            c.opening = ReadOpening(d04.SectionOf(1), c);
            c.clothFixed = ReadClothFixed(d03.SectionOf(2), d03.SectionOf(3), d03.SectionOf(4));
            ReadUnlocks(d10.SectionOf(1), c);
            return c;
        }

        /// <summary>从某个目录向上找含 docs/04 的工程根目录。Unity 编辑器的工作目录就是工程根。</summary>
        public static string FindProjectRoot(string startDirectory)
        {
            var dir = new DirectoryInfo(startDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "docs", Doc04))) return dir.FullName;
                dir = dir.Parent;
            }
            throw new DocParseException("从 " + startDirectory + " 向上找不到 docs/" + Doc04);
        }

        // ---- docs/04 §8 品种与分组 ----

        static void ReadVarieties(Section s8, ConfigSnapshot c)
        {
            foreach (var r in s8.Table("组"))
            {
                if (!GroupIds.TryGetValue(r[0], out var gid))
                    throw new DocParseException(s8.where + " 分组表出现未知组「" + r[0] + "」");
                var g = new StretchGroupRow { id = gid, name = r[0] };
                Num.Range(r[1], s8.where, out g.distanceMin, out g.distanceMax);
                Num.Range(r[2], s8.where, out g.dampingMin, out g.dampingMax);
                c.stretchGroups.Add(g);
            }

            foreach (var r in s8.Table("品种"))
            {
                string name = r[0];
                if (!VarietyIds.TryGetValue(name, out var id))
                    throw new DocParseException(s8.where + " 出现未登记的品种「" + name + "」，先在 docs/05 定 id");
                if (!VarietyGroup.TryGetValue(name, out var group))
                    throw new DocParseException(s8.where + " 的品种「" + name + "」不属于任何分组");
                var v = new VarietyRow
                {
                    id = id,
                    name = name,
                    group = group,
                    stretchGroup = group,
                    idProvisional = !HandbookVarietyIds.Contains(id),
                    launch = IsOpen(r[6]),
                    liningOnly = r[6].Contains("仅衬里"),
                };
                Num.Range(r[1], s8.where, out v.densityMin, out v.densityMax);
                Num.Range(r[2], s8.where, out v.bendMin, out v.bendMax);
                Num.Range(r[3], s8.where, out v.windMin, out v.windMax);
                Num.Range(r[4], s8.where, out v.glossMin, out v.glossMax);
                Num.Range(r[5], s8.where, out v.opacityMin, out v.opacityMax);
                c.varieties.Add(v);
                if (v.idProvisional) c.notes.Add("品种「" + name + "」的 id " + id + " 为暂定，docs/05 §2 未给出");
            }
        }

        // ---- docs/05 §3 染料 ----

        static void ReadDyes(Section s05, Section s04season, ConfigSnapshot c)
        {
            var idCell = s05.Row("字段", "id")[1];
            var launchCell = s05.Row("字段", "launch")[1];
            var launchNames = new HashSet<string>(
                Regex.Replace(launchCell, "为\\s*true.*$", "").Split(new[] { '、' }, StringSplitOptions.RemoveEmptyEntries));
            var chapter = Regex.Match(launchCell, "工艺章另开([^；。|]+)");
            if (chapter.Success)
                foreach (var n in chapter.Groups[1].Value.Split(new[] { '、' }, StringSplitOptions.RemoveEmptyEntries)) launchNames.Add(n.Trim());

            var best = new Dictionary<string, string>();
            foreach (var r in s04season.Table("季"))
            {
                if (!Seasons.TryGetValue(r[0], out var season)) continue;
                foreach (var n in r[3].Split(new[] { '、' }, StringSplitOptions.RemoveEmptyEntries))
                    if (!best.ContainsKey(n.Trim())) best[n.Trim()] = season;
            }

            foreach (Match m in Regex.Matches(idCell, "`(\\w+)`"))
            {
                string id = m.Groups[1].Value;
                if (!DyeNames.TryGetValue(id, out var name))
                    throw new DocParseException(s05.where + " 出现未登记的染料 id " + id);
                c.dyes.Add(new DyeRow
                {
                    id = id,
                    name = name,
                    role = MordantDyes.Contains(id) ? "mordant" : "color",
                    launch = launchNames.Contains(name),
                    bestSeason = best.TryGetValue(name, out var season) ? season : "any",
                });
            }
        }

        static string DyeIdByLabel(string label)
        {
            foreach (var kv in DyeNames)
                if (kv.Value == label) return kv.Key;
            return null;
        }

        static string DyeIdByName(ConfigSnapshot c, string name)
        {
            var d = c.dyes.Find(x => x.name == name);
            return d == null ? null : d.id;
        }

        // ---- 形制 ----

        static void ReadPattern(Section s07, Section s04day, ConfigSnapshot c)
        {
            var m = s07.MatchAll("首发形制只有(\\S+?)：(\\S+?)。");
            AddPattern(m.Groups[1].Value, "汉", m.Groups[2].Value, s07, s04day, c);
            // 工艺章形制：名（朝代）：部件。（docs/07 §3）
            foreach (Match cm in Regex.Matches(s07.text, "(\\p{IsCJKUnifiedIdeographs}+)（(\\p{IsCJKUnifiedIdeographs})）：([^。]+)。"))
                AddPattern(cm.Groups[1].Value, cm.Groups[2].Value, cm.Groups[3].Value, s07, s04day, c);
        }

        static void AddPattern(string patternName, string dynastyLabel, string partsText, Section s07, Section s04day, ConfigSnapshot c)
        {
            if (!PatternNames.TryGetValue(patternName, out var pid))
                throw new DocParseException(s07.where + " 的形制「" + patternName + "」没有 id");
            if (!Dynasties.TryGetValue(dynastyLabel, out var dyn))
                throw new DocParseException(s07.where + " 的形制「" + patternName + "」朝代「" + dynastyLabel + "」未知");
            var p = new PatternRow { id = pid, name = patternName, dynasty = dyn, launch = true };
            foreach (var raw in partsText.Split('、'))
            {
                bool optional = raw.StartsWith("可选", StringComparison.Ordinal);
                string n = optional ? raw.Substring(2) : raw;
                if (!SlotNames.TryGetValue(n, out var slot))
                    throw new DocParseException(s07.where + " 的部件「" + n + "」没有槽位");
                p.parts.Add(slot);
                if (optional) p.optionalParts.Add(slot);
            }
            var usage = s04day.Match(patternName + "用量：([^。]+)。");
            foreach (Match u in Regex.Matches(usage, "(\\p{IsCJKUnifiedIdeographs}+)\\s*" + Num.Unsigned))
            {
                if (!SlotNames.TryGetValue(u.Groups[1].Value, out var slot))
                    throw new DocParseException(s04day.where + " 用量中的「" + u.Groups[1].Value + "」没有槽位");
                p.partLengths.Add(new SlotLength { slot = slot, length = Num.Parse(u.Groups[2].Value, s04day.where) });
            }
            c.patterns.Add(p);
        }

        /// <summary>开放列：「是」为首发，「工艺章」为首发后第一章；「否」不开放。</summary>
        static bool IsOpen(string cell) => cell.StartsWith("是", StringComparison.Ordinal) || cell.StartsWith("工艺章", StringComparison.Ordinal);

        // ---- docs/16 角色 ----

        static void ReadCharacters(MarkdownDoc d16, ConfigSnapshot c)
        {
            var s2 = d16.SectionOf(2);
            foreach (var r in s2.Table("id"))
            {
                var ch = new CharacterRow { id = r[0], name = r[1], enabled = true, preferDynasty = "" };
                ch.template = Template(r[2], s2.where);
                ParsePrefs(r[3], ch, c);
                foreach (Match sm in Regex.Matches(r[3], "首发没有(\\S+?)时，(\\S+?)也算品种命中"))
                {
                    var from = MatchVariety(sm.Groups[1].Value, c);
                    var to = MatchVariety(sm.Groups[2].Value, c);
                    if (from == null || to == null) throw new DocParseException(s2.where + " 代计品种「" + sm.Value + "」无法对上 id");
                    ch.substitutes.Add(new VarietySubstitute { whenMissing = from.id, countsAs = to.id });
                }
                int max = 0;
                foreach (Match m in Regex.Matches(r[4], "\\d+")) max = Math.Max(max, int.Parse(m.Value));
                ch.launchTierMax = max;
                c.characters.Add(ch);
            }
            var s3 = d16.SectionOf(3);
            foreach (var r in s3.Table("id"))
            {
                if (!ClosedCharacterNames.TryGetValue(r[0], out var cname) || !s3.text.Contains(cname))
                    throw new DocParseException(s3.where + " 的角色 " + r[0] + " 找不到对应的中文名");
                var ch = new CharacterRow { id = r[0], name = cname, enabled = false, launchTierMax = 0, preferDynasty = "" };
                ch.template = Template(r[1], s3.where);
                ParsePrefs(r[2], ch, c);
                c.characters.Add(ch);
            }
            c.notes.Add("docs/11 的篇章角色没有 id，未入表；它们默认关闭，见 docs/11 §1");
        }

        static string Template(string label, string where)
        {
            if (!Templates.TryGetValue(label, out var t))
                throw new DocParseException(where + " 的模板「" + label + "」不在 docs/05 §5 的取值内");
            return t;
        }

        static void ParsePrefs(string cell, CharacterRow ch, ConfigSnapshot c)
        {
            var leftovers = new List<string>();
            foreach (var raw in cell.Split(new[] { '、', '，', '。' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string token = raw.Trim();
                var v = MatchVariety(token, c);
                if (v != null) { if (!ch.preferVariety.Contains(v.id)) ch.preferVariety.Add(v.id); }
                else leftovers.Add(token);
            }
            ch.note = string.Join("；", leftovers);
        }

        /// <summary>先按全名，再按唯一的名称前缀（如「妆花」→ 妆花缎）。</summary>
        static VarietyRow MatchVariety(string token, ConfigSnapshot c)
        {
            var exact = c.varieties.Find(v => v.name == token);
            if (exact != null) return exact;
            if (token.Length < 2) return null;
            var prefix = c.varieties.FindAll(v => v.name.StartsWith(token, StringComparison.Ordinal));
            return prefix.Count == 1 ? prefix[0] : null;
        }

        // ---- docs/05 §6 舞台 ----

        static void ReadStages(Section s6, ConfigSnapshot c)
        {
            foreach (var r in s6.Table("id"))
            {
                var m = Regex.Match(r[0], "`?(\\w+)`?");
                c.stages.Add(new StageRow { id = m.Groups[1].Value, launch = r[1].Trim() == "true" });
            }
        }

        // ---- docs/10 §2 教学委托 ----

        static void ReadQuests(Section s2, ConfigSnapshot c)
        {
            foreach (var line in s2.lines)
            {
                var m = Regex.Match(line, "^(\\d+)\\.\\s*(.+)$");
                if (!m.Success) continue;
                int order = int.Parse(m.Groups[1].Value);
                c.quests.Add(new QuestRow
                {
                    id = "tutorial_" + order,
                    tutorial = true,
                    order = order,
                    description = m.Groups[2].Value.Trim(),
                });
            }
            if (c.quests.Count == 0) throw new DocParseException(s2.where + " 没有读到教学委托");
            c.notes.Add("教学委托 id（tutorial_1…）为暂定，docs/05 §7 未给出");
        }

        // ---- docs/04 §1 开局 ----

        static OpeningData ReadOpening(Section s1, ConfigSnapshot c)
        {
            string w = s1.where;
            var o = new OpeningData();
            o.silkCoin = Num.FirstInt(s1.Row("资源", "丝钱")[1], w);
            var xpCell = s1.Row("资源", "工艺经验")[1];
            o.xp = Num.FirstInt(xpCell, w);
            o.level = (int)Num.Parse(Req(xpCell, "等级\\s*(\\d+)", w), w);
            o.xiShiAffection = Num.FirstInt(s1.Row("资源", "西施好感")[1], w);

            var boltCell = s1.Row("物品", "布 A")[1];
            var bt = boltCell.Split('，');
            var variety = c.varieties.Find(v => v.name == bt[0].Trim());
            if (variety == null) throw new DocParseException(w + " 布 A 的品种「" + bt[0] + "」不在 §8");
            o.bolt.variety = variety.id;
            o.bolt.fineness = FinenessOf(Req(boltCell, "(\\S)丝", w), w);
            o.bolt.materialScore = (int)Num.Parse(Req(boltCell, "原料分\\s*(\\d+)", w), w);
            o.bolt.yarnScore = (int)Num.Parse(Req(boltCell, "成纱分\\s*(\\d+)", w), w);
            o.bolt.weaveScore = (int)Num.Parse(Req(boltCell, "织造分\\s*(\\d+)", w), w);
            o.bolt.dyed = !boltCell.Contains("未染");
            o.bolt.length = Num.Parse(Req(boltCell, Num.Unsigned + "\\s*米", w), w);
            o.bolt.width = Num.Parse(Req(boltCell, "幅宽\\s*(\\d+)", w), w);

            var yarnCell = s1.Row("物品", "丝 B")[1];
            var yt = yarnCell.Split('，');
            if (!Fibers.TryGetValue(yt[0].Trim(), out var fiber))
                throw new DocParseException(w + " 丝 B 的纤维「" + yt[0] + "」没有 id");
            o.yarn.fiber = fiber;
            o.yarn.fineness = FinenessOf(yt[1].Trim(), w);
            o.yarn.processScore = (int)Num.Parse(Req(yarnCell, "过程分\\s*(\\d+)", w), w);
            o.yarn.materialScore = (int)Num.Parse(Req(yarnCell, "原料分\\s*(\\d+)", w), w);
            o.yarn.joints = (int)Num.Parse(Req(yarnCell, "接头\\s*(\\d+)", w), w);
            o.yarn.length = Num.Parse(Req(yarnCell, Num.Unsigned + "\\s*米", w), w);

            var cocoonCell = s1.Row("物品", "茧")[1];
            o.trayEmpty = cocoonCell.Contains("蚕箔为空");
            var seasonLabel = Req(cocoonCell, "季节为(\\S)", w);
            if (!Seasons.TryGetValue(seasonLabel, out o.season))
                throw new DocParseException(w + " 季节「" + seasonLabel + "」未知");

            var dyeCell = s1.Row("物品", "染料")[1];
            foreach (Match m in Regex.Matches(dyeCell, "干(\\p{IsCJKUnifiedIdeographs}+?)\\s*(\\d+)\\s*份"))
            {
                string id = DyeIdByName(c, m.Groups[1].Value);
                if (id == null) throw new DocParseException(w + " 开局染料「" + m.Groups[1].Value + "」不在 docs/05 §3");
                o.dyes.Add(new DyeCount { dyeId = id, count = int.Parse(m.Groups[2].Value), state = "dry" });
            }
            if (o.dyes.Count == 0) throw new DocParseException(w + " 没有读到开局染料");
            return o;
        }

        static string FinenessOf(string label, string where)
        {
            if (!Fineness.TryGetValue(label, out var f))
                throw new DocParseException(where + " 细度「" + label + "」未知");
            return f;
        }

        static string Req(string text, string pattern, string where)
        {
            var m = Regex.Match(text, pattern);
            if (!m.Success) throw new DocParseException(where + " 中「" + text + "」找不到：" + pattern);
            return m.Groups[1].Value;
        }

        // ---- docs/03 §2 固定项 ----

        static ClothFixed ReadClothFixed(Section s2, Section s3, Section s4)
        {
            return new ClothFixed
            {
                gravityFalloff = s2.Number("gravityFalloff`\\s*=\\s*" + Num.Unsigned),
                triangleBendingStiffness = s2.Number("triangleBendingConstraint\\.stiffness`\\s*=\\s*" + Num.Unsigned),
                radius = s2.Number("`radius`\\s*=\\s*" + Num.Unsigned),
                alphaPerOpacity = s4.Number("α\\s*=\\s*1\\s*[−-]\\s*透明度²\\s*×\\s*" + Num.Unsigned),
                smoothnessPerGloss = s4.Number("平滑度：光泽\\s*×\\s*" + Num.Unsigned),
                museumNearMeters = s3.Number("走近展柜\\s*" + Num.Unsigned + "\\s*米内"),
            };
        }

        // ---- docs/10 §1 等级解锁 ----

        static void ReadUnlocks(Section s1, ConfigSnapshot c)
        {
            foreach (var r in s1.Table("等级"))
            {
                var u = new UnlockRow
                {
                    level = int.Parse(r[0]),
                    raw = r[1],
                    launch = IsOpen(r[2]),
                };
                var lv = c.balance.economy.levels.Find(l => l.level == u.level);
                if (lv == null) throw new DocParseException(s1.where + " 的等级 " + u.level + " 在 docs/04 §7 没有经验门槛");
                u.xpRequired = lv.xp;

                foreach (var part in r[1].Split('、'))
                {
                    var tiers = new List<int>();
                    foreach (Match m in Regex.Matches(part, "\\d+")) tiers.Add(int.Parse(m.Value));
                    foreach (var piece in part.Split('与'))
                    {
                        string token = Regex.Replace(piece, "的?\\s*[\\d/]+\\s*档?$", "").Trim();
                        if (token.Length == 0) continue;
                        var ch = c.characters.Find(x => token.StartsWith(x.name, StringComparison.Ordinal));
                        if (ch != null)
                        {
                            u.characters.Add(new CharacterTierUnlock { characterId = ch.id, tiers = new List<int>(tiers) });
                            continue;
                        }
                        var v = MatchVariety(token, c);
                        if (v != null) { u.varieties.Add(v.id); continue; }
                        string dye = DyeIdByName(c, token);
                        if (dye != null) { u.dyes.Add(dye); continue; }
                        if (PatternNames.TryGetValue(token, out var pid)) { u.patterns.Add(pid); continue; }
                        u.other.Add(token);
                    }
                }
                c.unlocks.Add(u);
            }
        }

        // ---- docs/04 §2–§11 ----

        static BalanceData ReadBalance(MarkdownDoc d04)
        {
            var b = new BalanceData();
            ReadDay(d04.SectionOf(2), b.day);
            ReadSeason(d04.SectionOf(3), b.season);
            ReadQuality(d04.SectionOf(4), b.quality);
            var s5 = d04.SectionOf(5);
            ReadPenalties(s5, b.penalties);
            ReadWeave(s5, b.weave);
            ReadDye(s5, b.dye);
            ReadAffection(d04.SectionOf(6), b.affection);
            ReadEconomy(d04.SectionOf(7), b.economy);
            ReadWindLight(d04.SectionOf(9), b.windLight);
            ReadExhibit(d04.SectionOf(10), b.exhibit);
            var s11 = d04.SectionOf(11);
            b.clothDecimals = ChineseDigit(s11.Match("保留(\\S)位小数"), s11.where);
            return b;
        }

        static void ReadDay(Section s, DayData day)
        {
            day.hoursPerDay = (int)s.Number("一天\\s*(\\d+)\\s*工时");
            day.teachingDayHours = (int)s.Number("教学日，工时为\\s*(\\d+)");
            foreach (var r in s.Table("工序"))
            {
                if (!HourKeys.TryGetValue(r[0], out var key))
                    throw new DocParseException(s.where + " 工时表出现未登记的工序「" + r[0] + "」");
                day.hours.Add(new HourCost { key = key, label = r[0], hours = Num.FirstInt(r[1], s.where) });
            }
            day.cancelMinHours = (int)s.Number("取消工序[^。]*至少\\s*(\\d+)");
            day.boltLength = s.Number("一匹布长度\\s*" + Num.Unsigned + "\\s*米");
            var ratio = s.MatchAll(Num.Unsigned + "\\s*米纱织\\s*" + Num.Unsigned + "\\s*米布");
            day.clothPerYarnMeter = Num.Parse(ratio.Groups[2].Value, s.where) / Num.Parse(ratio.Groups[1].Value, s.where);
            day.reelYieldMeters = s.Number("缫丝一束得纱\\s*" + Num.Unsigned + "\\s*米");
            day.widthDefault = s.Number("幅宽默认\\s*" + Num.Unsigned);
            var m = s.MatchAll("允许\\s*" + Num.Unsigned + "\\s*[–-]\\s*" + Num.Unsigned);
            day.widthMin = Num.Parse(m.Groups[1].Value, s.where);
            day.widthMax = Num.Parse(m.Groups[2].Value, s.where);
        }

        static void ReadSeason(Section s, SeasonData season)
        {
            season.springBase = Num.FirstInt(s.Row("季", "春")[1], s.where);
            season.summerHeatPerDay = Num.FirstInt(s.Row("季", "夏")[2], s.where);
            season.feedGoodUniformity = (int)s.Number("喂叶恰当\\s*" + Num.Signed);
            season.overfeedPenalty = (int)s.Number("喂过量\\s*" + Num.Signed);
            season.crowdingShapePenalty = (int)s.Number("上蔟过密\\s*" + Num.Signed);
            season.seedPerTray = (int)s.Number("收蚁耗蚕种\\s*(\\d+)\\s*份");
            season.trayBatch = (int)s.Number("批量\\s*(\\d+)\\s*束");
            season.reelBeats = (int)s.Number("缫丝一束\\s*(\\d+)\\s*拍");
            season.rotationFromLevel = Num.FirstInt(s.Row("项", "轮转开始等级")[1], s.where);
            season.daysPerSeason = Num.FirstInt(s.Row("项", "每季工坊日")[1], s.where);
            season.rows.Clear();
            foreach (var kv in new[] { "春", "夏", "秋", "冬" })
            {
                var r = s.Row("季", kv);
                var row = new SeasonRow { id = Seasons[kv], label = kv };
                row.cocoonBase = r[1].Contains("不结茧") ? -1 : Num.FirstInt(r[1], s.where);
                row.heatPerDay = Regex.IsMatch(r[2], "\\d") ? Num.FirstInt(r[2], s.where) : 0;
                foreach (var part in r[3].Split('、'))
                {
                    string n = part.Trim();
                    if (n == "幼靛") n = "靛蓝"; // docs/04 §3：幼靛记作靛蓝
                    foreach (var d in DyeNames) if (d.Value == n && !row.freshDyes.Contains(d.Key)) row.freshDyes.Add(d.Key);
                }
                season.rows.Add(row);
            }
        }

        static void ReadQuality(Section s, QualityData q)
        {
            string formula = s.Match("Q\\s*=\\s*([^\\n]+)");
            var labels = new Dictionary<string, string>
            {
                { "原料", "material" }, { "成纱", "yarn" }, { "织造", "weave" }, { "染色", "dye" },
                { "后整理", "finish" }, { "裁剪", "cut" }, { "缝制", "sew" },
            };
            int found = 0;
            foreach (Match m in Regex.Matches(formula, "(\\S+?)×" + Num.Unsigned))
            {
                if (!labels.TryGetValue(m.Groups[1].Value, out var key))
                    throw new DocParseException(s.where + " 的 Q 公式出现未知项「" + m.Groups[1].Value + "」");
                double v = Num.Parse(m.Groups[2].Value, s.where);
                switch (key)
                {
                    case "material": q.weights.material = v; break;
                    case "yarn": q.weights.yarn = v; break;
                    case "weave": q.weights.weave = v; break;
                    case "dye": q.weights.dye = v; break;
                    case "finish": q.weights.finish = v; break;
                    case "cut": q.weights.cut = v; break;
                    case "sew": q.weights.sew = v; break;
                }
                found++;
            }
            if (found != 7) throw new DocParseException(s.where + " 的 Q 公式应有 7 项，读到 " + found);

            q.undyedDyeScore = s.Number("未染色，染色分记\\s*" + Num.Unsigned);
            q.unfinishedFinishScore = s.Number("未后整理，后整理分记\\s*" + Num.Unsigned);
            var lw = s.MatchAll("外\\s*" + Num.Unsigned + "，中\\s*" + Num.Unsigned + "，里\\s*" + Num.Unsigned);
            q.layerOuter = Num.Parse(lw.Groups[1].Value, s.where);
            q.layerMiddle = Num.Parse(lw.Groups[2].Value, s.where);
            q.layerInner = Num.Parse(lw.Groups[3].Value, s.where);

            var tFormula = new Regex("^(?:" + Num.Unsigned + "\\s*\\+\\s*)?\\(?\\s*Q\\s*(?:-\\s*" + Num.Unsigned + ")?\\s*\\)?\\s*/\\s*"
                                     + Num.Unsigned + "\\s*×\\s*" + Num.Unsigned + "$");
            foreach (var r in s.Table("Q"))
            {
                if (!Tiers.TryGetValue(r[1], out var tier))
                    throw new DocParseException(s.where + " 档位「" + r[1] + "」未知");
                var band = new TierBand { tier = tier, label = r[1] };
                double lo, hi;
                Num.Range(r[0], s.where, out lo, out hi);
                band.qMin = (int)lo;
                band.qMax = (int)hi;
                var m = tFormula.Match(Num.Normalize(r[2]));
                if (!m.Success) throw new DocParseException(s.where + " 的 t 公式「" + r[2] + "」无法读出");
                band.tBase = m.Groups[1].Success ? Num.Parse(m.Groups[1].Value, s.where) : 0;
                band.qOffset = m.Groups[2].Success ? Num.Parse(m.Groups[2].Value, s.where) : 0;
                band.denominator = Num.Parse(m.Groups[3].Value, s.where);
                band.span = Num.Parse(m.Groups[4].Value, s.where);
                q.bands.Add(band);
            }

            string launchDynasty = s.Match("首发[^。]*只用(\\S)偏移");
            foreach (var r in s.Table("朝代"))
            {
                if (!Dynasties.TryGetValue(r[0], out var dyn))
                    throw new DocParseException(s.where + " 朝代「" + r[0] + "」未知");
                var o = new DynastyOffset { dynasty = dyn, label = r[0], parsed = true };
                double[] vals = new double[4];
                for (int i = 0; i < 4; i++)
                {
                    var m = Regex.Match(r[i + 1], "^×" + Num.Unsigned + "$");
                    if (m.Success) vals[i] = Num.Parse(m.Groups[1].Value, s.where);
                    else o.parsed = false;
                }
                if (o.parsed) { o.density = vals[0]; o.bend = vals[1]; o.wind = vals[2]; o.gloss = vals[3]; }
                o.launch = o.parsed && (r[0] == launchDynasty || Regex.IsMatch(s.text, r[0] + "的行在工艺章启用"));
                q.dynasties.Add(o);
            }

            foreach (var r in s.Table("模式"))
            {
                if (!PresentModes.TryGetValue(r[0], out var mode))
                    throw new DocParseException(s.where + " 呈现模式「" + r[0] + "」未知");
                var p = new PresentationClamp { mode = mode, label = r[0] };
                var tMax = Regex.Match(r[1], "t\\s*≤\\s*" + Num.Unsigned);
                if (tMax.Success) { p.hasTMax = true; p.tMax = Num.Parse(tMax.Groups[1].Value, s.where); }
                var tMin = Regex.Match(r[1], "t\\s*≥\\s*" + Num.Unsigned);
                if (tMin.Success) { p.hasTMin = true; p.tMin = Num.Parse(tMin.Groups[1].Value, s.where); }
                var gl = Regex.Match(r[1], "光泽再\\s*×" + Num.Unsigned);
                if (gl.Success) p.glossFactor = Num.Parse(gl.Groups[1].Value, s.where);
                var wf = Regex.Match(r[1], "薄纱风力再\\s*×" + Num.Unsigned);
                if (wf.Success) p.gauzeWindFactor = Num.Parse(wf.Groups[1].Value, s.where);
                p.syncCapPercent = Num.FirstInt(r[2], s.where);
                q.presentation.Add(p);
            }
        }

        static void ReadPenalties(Section s, PenaltyData p)
        {
            const string H = "事件";
            string w = s.where;
            p.reelBreak = Num.FirstInt(s.RowStarting(H, "缫丝断头")[1], w);
            var chaos = s.RowStarting(H, "织造一段中");
            p.chaosRatioThreshold = Num.Parse(Req(chaos[0], "超过\\s*" + Num.Unsigned + "%", w), w) / 100.0;
            p.chaosSegmentCap = Num.FirstInt(chaos[1], w);
            p.wrongPatternMotifScore = Num.FirstInt(s.RowStarting(H, "花本选错")[1], w);
            p.orientation90 = Num.FirstInt(Req(s.RowStarting(H, "纹样方向")[1], "裁剪\\s*" + Num.Signed, w), w);
            p.seamFront = Num.FirstInt(Req(s.RowStarting(H, "拼缝落在")[1], "裁剪\\s*" + Num.Signed, w), w);
            p.legendaryInnerWaste = Num.FirstInt(Req(s.RowStarting(H, "传世布做里层")[1], "裁剪\\s*" + Num.Signed, w), w);
            p.sewOrder = Num.FirstInt(Req(s.RowStarting(H, "缝序错")[1], "缝制\\s*" + Num.Signed, w), w);
            p.formSkipped = Num.FirstInt(Req(s.RowStarting(H, "未打开人台")[1], "缝制\\s*" + Num.Signed, w), w);
            p.sewLowThreshold = (int)Num.Parse(Req(s.RowStarting(H, "缝制分")[0], "<\\s*(\\d+)", w), w);
            p.edgeGlossPerPoint = Num.Parse(Req(s.RowStarting(H, "边损每")[1], "光泽\\s*" + Num.Signed, w), w);
            p.edgeNoLegendaryOuter = (int)Num.Parse(Req(s.RowStarting(H, "边损 ≥")[0], "≥\\s*(\\d+)", w), w);
            p.unpickEdgeDamage = Num.FirstInt(Req(s.Row(H, "拆衣")[1], "边损\\s*" + Num.Signed, w), w);
        }

        static void ReadWeave(Section s, WeaveData wv)
        {
            wv.rhythm = s.Number("节奏×" + Num.Unsigned);
            wv.densityStable = s.Number("密度稳定×" + Num.Unsigned);
            wv.motif = s.Number("花位×" + Num.Unsigned);
            wv.noMotifScore = s.Number("无花位时花位按\\s*" + Num.Unsigned);
            wv.steadyWindowMs = (int)s.Number("±(\\d+)\\s*毫秒为稳");
            wv.offWindowMs = (int)s.Number("±(\\d+)\\s*毫秒为偏");
            var beat = s.MatchAll("节拍分：稳\\s*" + Num.Unsigned + "，偏\\s*" + Num.Unsigned + "，乱\\s*" + Num.Unsigned);
            wv.beatInterval = s.Number("节拍间隔\\s*" + Num.Unsigned + "\\s*秒");
            wv.beatSteady = Num.Parse(beat.Groups[1].Value, s.where);
            wv.beatOff = Num.Parse(beat.Groups[2].Value, s.where);
            wv.beatChaos = Num.Parse(beat.Groups[3].Value, s.where);
            wv.beatsPerSegment = (int)s.Number("每段\\s*(\\d+)\\s*拍");
            wv.segments = ChineseDigit(s.Match("([一二三四五六七八九])段共"), s.where);
            wv.sewNeedlesPerPart = (int)s.Number("每个部件\\s*(\\d+)\\s*针");
            wv.cutStartScore = s.Number("裁剪分从\\s*" + Num.Unsigned + "\\s*起扣");
            wv.defectLine = s.Number("残布线：织造分\\s*<\\s*" + Num.Unsigned);
        }

        static void ReadDye(Section s, DyeData d)
        {
            var c = s.MatchAll("淡\\s*" + Num.Unsigned + "，中\\s*" + Num.Unsigned + "，浓\\s*" + Num.Unsigned);
            d.concentration.Add(new NamedValue { key = "light", label = "淡", value = Num.Parse(c.Groups[1].Value, s.where) });
            d.concentration.Add(new NamedValue { key = "medium", label = "中", value = Num.Parse(c.Groups[2].Value, s.where) });
            d.concentration.Add(new NamedValue { key = "strong", label = "浓", value = Num.Parse(c.Groups[3].Value, s.where) });
            d.strengthCap = s.Number("强度\\s*=\\s*min\\(\\s*" + Num.Unsigned);
            var sc = s.MatchAll("温度吻合×" + Num.Unsigned + "\\s*\\+\\s*时间吻合×" + Num.Unsigned + "\\s*\\+\\s*搅拌稳定×" + Num.Unsigned);
            d.scoreTemp = Num.Parse(sc.Groups[1].Value, s.where);
            d.scoreTime = Num.Parse(sc.Groups[2].Value, s.where);
            d.scoreStir = Num.Parse(sc.Groups[3].Value, s.where);
            d.colorMix = s.Number("强度×" + Num.Unsigned);
            d.maxLayers = (int)s.Number("最多\\s*(\\d+)\\s*层");
            d.freshConcentrationFactor = s.Number("新鲜染料浓度系数\\s*×\\s*" + Num.Unsigned);
            d.overflowPenalty = (int)s.Number("挤掉最早一层，染色分\\s*" + Num.Signed);
            d.costPerBolt = (int)s.Number("每浸染一匹耗所选干染料\\s*(\\d+)\\s*份");
            var temp = s.Row("操作", "水温")[1];
            d.bestTemp = Req(temp, "最佳为(\\S)", s.where);
            d.tempMatchBest = Num.Parse(Req(temp, "最佳档吻合\\s*" + Num.Unsigned, s.where), s.where);
            d.tempMatchOther = Num.Parse(Req(temp, "其余\\s*" + Num.Unsigned, s.where), s.where);
            var lift = s.Row("操作", "起布")[1];
            d.liftBandWidth = Num.Parse(Req(lift, "提示带宽\\s*" + Num.Unsigned, s.where), s.where);
            foreach (var kv in new[] { new[] { "light", "淡" }, new[] { "medium", "中" }, new[] { "strong", "浓" } })
                d.liftCenter.Add(new NamedValue { key = kv[0], label = kv[1], value = Num.Parse(Req(lift, kv[1] + "\\s*" + Num.Unsigned + "\\s*秒", s.where), s.where) });
            d.liftPenaltyPerSecond = Num.Parse(Req(lift, "每偏\\s*1\\s*秒减\\s*" + Num.Unsigned, s.where), s.where);
            d.liftMatchMin = Num.Parse(Req(lift, "最低\\s*" + Num.Unsigned, s.where), s.where);
            d.stirOffWeight = Num.Parse(Req(s.Row("操作", "搅拌")[1], "偏拍数\\s*×\\s*" + Num.Unsigned, s.where), s.where);
            d.unevenAllowedMax = s.Number("不均\\s*≤\\s*" + Num.Unsigned + "\\s*为配方允许");
            d.livelyMin = s.Number("其中\\s*≥\\s*" + Num.Unsigned + "\\s*可得");
            foreach (var r in s.Table("染料"))
            {
                string id = r[0] == "未染丝底色" ? "base" : DyeIdByLabel(r[0]);
                var hex = Regex.Match(r[1], "#([0-9A-Fa-f]{6})");
                if (id == null || !hex.Success) throw new DocParseException(s.where + " 染料色「" + r[0] + "」无法读出");
                int v = Convert.ToInt32(hex.Groups[1].Value, 16);
                d.colors.Add(new DyeColor { dyeId = id, label = r[0], r = ((v >> 16) & 255) / 255.0, g = ((v >> 8) & 255) / 255.0, b = (v & 255) / 255.0 });
            }
            d.opacityPerLayer = s.Number("透明度系数\\s*=\\s*1\\s*[−-]\\s*" + Num.Unsigned + "\\s*×\\s*层数");

            foreach (var r in s.Table("处理"))
            {
                if (!ModKeys.TryGetValue(r[0], out var key))
                    throw new DocParseException(s.where + " 染色修正表出现未登记的处理「" + r[0] + "」");
                var m = new TreatmentMod { key = key, label = r[0] };
                m.damping = Num.First(r[1], s.where);
                var factor = Regex.Match(r[2], "×" + Num.Unsigned);
                if (factor.Success) { m.bendFactor = Num.Parse(factor.Groups[1].Value, s.where); m.bend = 0; }
                else m.bend = Num.First(r[2], s.where);
                m.gloss = Num.First(r[3], s.where);
                d.mods.Add(m);
            }
        }

        static void ReadAffection(Section s, AffectionData a)
        {
            foreach (var r in s.Table("事件"))
            {
                string label = r[0];
                string key;
                if (label.StartsWith("契合 ≥", StringComparison.Ordinal))
                {
                    key = "fitHigh";
                    a.fitHighMin = (int)Num.Parse(Req(label, "≥\\s*(\\d+)", s.where), s.where);
                }
                else if (Regex.IsMatch(label, "^契合\\s*\\d+\\s*[–-]"))
                {
                    key = "fitMid";
                    a.fitMidMin = (int)Num.Parse(Req(label, "^契合\\s*(\\d+)", s.where), s.where);
                }
                else if (label.StartsWith("契合 <", StringComparison.Ordinal)) key = "fitLow";
                else if (label.StartsWith("主题委托", StringComparison.Ordinal)) key = "themeQuest";
                else if (label.StartsWith("同一件传世衣", StringComparison.Ordinal)) key = "legendaryReplay";
                else throw new DocParseException(s.where + " 好感表出现未登记的事件「" + label + "」");
                a.events.Add(new AffectionEvent { key = key, label = label, value = Num.FirstInt(r[1], s.where) });
            }

            var th = s.MatchAll("门槛：(\\d+)\\s*打开基础演出，(\\d+)\\s*打开互动，(\\d+)\\s*打开");
            for (int i = 1; i <= 3; i++) a.tierThresholds.Add(int.Parse(th.Groups[i].Value));

            a.fitVariety = (int)s.Number("品种命中\\s*\\+(\\d+)");
            a.fitDynasty = (int)s.Number("朝代或文化圈命中\\s*\\+(\\d+)");
            a.fitTrait = (int)s.Number("词条命中\\s*\\+(\\d+)");
            a.fitNote = (int)s.Number("符合备注\\s*\\+(\\d+)");
            a.fitBase = (int)s.Number("四项都没有则为\\s*(\\d+)");

            var heat = s.MatchAll("热度\\s*=\\s*Q×" + Num.Unsigned + "\\s*\\+\\s*契合×" + Num.Unsigned + "\\s*\\+\\s*阶段分×" + Num.Unsigned);
            a.heatQ = Num.Parse(heat.Groups[1].Value, s.where);
            a.heatFit = Num.Parse(heat.Groups[2].Value, s.where);
            a.heatStage = Num.Parse(heat.Groups[3].Value, s.where);

            string stages = s.Match("阶段分：([^。]+)。");
            foreach (Match m in Regex.Matches(stages, "(\\d+)\\s*档\\s*(\\d+)"))
                a.stageScores.Add(new TierScore { tier = int.Parse(m.Groups[1].Value), score = int.Parse(m.Groups[2].Value) });
            if (a.stageScores.Count == 0) throw new DocParseException(s.where + " 没有读到阶段分");

            a.maxTraits = (int)s.Number("词条最多\\s*(\\d+)\\s*个");
            foreach (var r in s.Table("词条"))
            {
                var t = new TraitRow { name = r[0], condition = r[1], effect = r[2] };
                foreach (Match m in Regex.Matches(r[1], "≥\\s*(\\d+)")) t.thresholds.Add(int.Parse(m.Groups[1].Value));
                a.traits.Add(t);
            }
        }

        static void ReadEconomy(Section s, EconomyData e)
        {
            const string H = "来源或去向";
            string w = s.where;
            e.startCoin = Num.FirstInt(s.Row(H, "开局")[1], w);
            var ex = s.Row(H, "展览每天结算")[1];
            var m = Regex.Match(ex, "每\\s*(\\d+)\\s*分折\\s*(\\d+)\\s*丝钱，上限\\s*(\\d+)");
            if (!m.Success) throw new DocParseException(w + " 展览折算「" + ex + "」无法读出");
            e.exhibitPointsPer = int.Parse(m.Groups[1].Value);
            e.exhibitCoinsPer = int.Parse(m.Groups[2].Value);
            e.exhibitCoinCap = int.Parse(m.Groups[3].Value);
            e.sellCommonPerMeter = (int)Num.Parse(Req(s.Row(H, "出售普通布")[1], "每米\\s*(\\d+)", w), w);
            e.dryDyePrice = Num.FirstInt(s.RowStarting(H, "干染料")[1], w);
            e.threadPrice = Num.FirstInt(s.Row(H, "缝线")[1], w);
            e.goldThreadPrice = Num.FirstInt(s.Row(H, "金线一小轴")[1], w);
            e.seedPrice = Num.FirstInt(s.Row(H, "蚕种")[1], w);
            e.freshDyePrice = Num.FirstInt(s.RowStarting(H, "当季新鲜染料")[1], w);

            var xp = s.MatchAll("工序工时\\s*×\\s*" + Num.Unsigned + "\\s*×\\s*品质系数。普通\\s*" + Num.Unsigned
                                + "，精良\\s*" + Num.Unsigned + "，传世\\s*" + Num.Unsigned);
            e.xpPerHour = Num.Parse(xp.Groups[1].Value, w);
            e.xpCommon = Num.Parse(xp.Groups[2].Value, w);
            e.xpFine = Num.Parse(xp.Groups[3].Value, w);
            e.xpLegendary = Num.Parse(xp.Groups[4].Value, w);
            if (!s.text.Contains("残次给一半")) throw new DocParseException(w + " 找不到残次经验的规定");
            e.xpDefectFactor = 0.5; // 「一半」

            foreach (var r in s.Table("等级"))
                e.levels.Add(new LevelRow
                {
                    level = int.Parse(r[0]),
                    xp = int.Parse(r[1]),
                    launch = r[2].StartsWith("是", StringComparison.Ordinal),
                });
        }

        static void ReadWindLight(Section s, WindLightData wl)
        {
            var bw = s.MatchAll("基础风\\s*" + Num.Unsigned + "\\s*[–-]\\s*" + Num.Unsigned);
            wl.baseWindMin = Num.Parse(bw.Groups[1].Value, s.where);
            wl.baseWindMax = Num.Parse(bw.Groups[2].Value, s.where);
            wl.tier30WindMax = s.Number("30\\s*档不超过\\s*" + Num.Unsigned);
            var peak = s.MatchAll("70\\s*档高潮\\s*" + Num.Unsigned + "\\s*[–-]\\s*" + Num.Unsigned);
            wl.tier70PeakMin = Num.Parse(peak.Groups[1].Value, s.where);
            wl.tier70PeakMax = Num.Parse(peak.Groups[2].Value, s.where);
            wl.tier70EndMax = s.Number("定格前降到\\s*" + Num.Unsigned + "\\s*以下");
            wl.maxConcurrentWinds = (int)s.Number("最多同时\\s*(\\d+)\\s*个");
            wl.rimPercentInit = (int)s.Number("取\\s*(\\d+)%\\s*作为初值");
            string temps = s.Match("主光色温初值：([^。]+)。");
            foreach (Match m in Regex.Matches(temps, "(\\S)\\s*(\\d+)K"))
                wl.colorTemps.Add(new ColorTemp { label = m.Groups[1].Value, kelvin = int.Parse(m.Groups[2].Value) });
        }

        static void ReadExhibit(Section s, ExhibitData e)
        {
            var m = s.MatchAll("平均品质×" + Num.Unsigned + "\\s*\\+\\s*主题一致×" + Num.Unsigned
                               + "\\s*\\+\\s*说明完整×" + Num.Unsigned + "\\s*\\+\\s*影像×" + Num.Unsigned);
            e.quality = Num.Parse(m.Groups[1].Value, s.where);
            e.theme = Num.Parse(m.Groups[2].Value, s.where);
            e.label = Num.Parse(m.Groups[3].Value, s.where);
            e.image = Num.Parse(m.Groups[4].Value, s.where);
            e.minItems = (int)s.Number("少于\\s*(\\d+)\\s*件不能开幕");
        }

        static int ChineseDigit(string ch, string where)
        {
            const string digits = "零一二三四五六七八九";
            int i = digits.IndexOf(ch, StringComparison.Ordinal);
            if (i < 0 && !int.TryParse(ch, out i)) throw new DocParseException(where + " 「" + ch + "」不是数字");
            return i;
        }
    }
}
