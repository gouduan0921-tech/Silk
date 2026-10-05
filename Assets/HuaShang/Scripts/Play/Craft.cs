using System;
using System.Collections.Generic;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>织、染、裁、缝、人台、拆衣（docs/07）。只在玩家确认时结算（docs/19 §3）。</summary>
    public static class Craft
    {
        public const string Tutorial1 = "tutorial_1";
        public const string Tutorial2 = "tutorial_2";
        public const string Tutorial3 = "tutorial_3";

        // ---------------- 织 ----------------

        /// <summary>教学第 1 步可以「直接确认库存」（docs/10 §2）：确认一匹绢，不新增布、不耗纱。</summary>
        public static Result ConfirmBolt(SaveRoot s, ConfigSnapshot c, string boltId)
        {
            var b = Find.Bolt(s, boltId);
            if (b == null) return Result.Fail("没有这匹布");
            if (b.variety == "juan") CompleteQuest(s, Tutorial1);
            return Result.Ok(b.id);
        }

        public class WeaveInput
        {
            public string yarnId;
            public string varietyId;
            /// <summary>花本 id：首发 "su"（素）或 "grid"（细方格）。</summary>
            public string patternId;
            /// <summary>花本是否选对（docs/07 §1）；素布不需要花位。</summary>
            public bool patternCorrect = true;
            /// <summary>按段给出的节拍判定，段数与每段拍数见 docs/04 §5。</summary>
            public List<List<Beat>> segments = new List<List<Beat>>();
        }

        public const string PatternPlain = "su";
        public const string PatternGrid = "grid";
        public const string PatternJacquard = "jacquard";
        public const string RuQun = "ruQun";

        /// <summary>织一匹的工时项：提花花本按提花，缎、绫按斜纹或素缎，其余平纹（docs/04 §2）。</summary>
        public static string WeaveHoursKey(VarietyRow v, string patternId)
        {
            if (patternId == PatternJacquard || (v != null && (v.group == "brocade" || v.id == "gaiJi" || v.id == "keSi"))) return "weaveJacquard";
            if (v != null && (v.name.Contains("缎") || v.name == "绫")) return "weaveTwillSatin";
            return "weavePlain";
        }

        /// <summary>花本是否选对：花缎、云锦、改机必须用提花，其他品种不限（docs/07 §1）。</summary>
        public static bool PatternFits(VarietyRow v, string patternId) => v == null || !NeedsJacquard(v.id) || patternId == PatternJacquard;

        /// <summary>三台织机（docs/07 §1）。</summary>
        public enum Loom { Plain, Satin, Draw }

        /// <summary>品种上哪台织机：提花与锦上花楼机，缎与绫上缎机，其余（含缂丝）上平纹机。</summary>
        public static Loom LoomOf(VarietyRow v)
        {
            if (v == null) return Loom.Plain;
            if (v.id == "keSi") return Loom.Plain;
            if (NeedsJacquard(v.id) || v.group == "brocade") return Loom.Draw;
            if (v.name.Contains("缎") || v.name == "绫") return Loom.Satin;
            return Loom.Plain;
        }

        /// <summary>织机在等级表里的开放词；平纹机无需开放。</summary>
        public static string LoomToken(Loom k) => k == Loom.Satin ? "缎机" : k == Loom.Draw ? "花楼机" : null;

        /// <summary>花缎、云锦、改机必须用提花花本（docs/07 §1）。</summary>
        public static bool NeedsJacquard(string varietyId) => varietyId == "huaDuan" || varietyId == "yunJin" || varietyId == "gaiJi";

        /// <summary>织造分 = 节奏×w + 密度稳定×w + 花位×w（docs/04 §5）。</summary>
        public static double WeaveScore(WeaveInput input, Yarn yarn, VarietyRow variety, ConfigSnapshot c)
        {
            var w = c.balance.weave;
            var p = c.balance.penalties;
            double rhythmSum = 0;
            var all = new List<Beat>();
            foreach (var seg in input.segments)
            {
                double segScore = Beats.Average(seg, w);
                if (Beats.Ratio(seg, Beat.Chaos) > p.chaosRatioThreshold) segScore = Math.Min(segScore, p.chaosSegmentCap);
                rhythmSum += segScore;
                all.AddRange(seg);
            }
            double rhythm = input.segments.Count > 0 ? rhythmSum / input.segments.Count : 0;
            double density = Beats.Ratio(all, Beat.Steady) * 100;
            double motif = input.patternId == PatternPlain || string.IsNullOrEmpty(input.patternId)
                ? w.noMotifScore
                : (input.patternCorrect ? 100 : p.wrongPatternMotifScore);
            double score = rhythm * w.rhythm + density * w.densityStable + motif * w.motif;
            if (yarn.fineness == "coarse" && variety.group == "gauze") score = Math.Min(score, w.coarseGauzeCap);
            return Math.Round(score, MidpointRounding.AwayFromZero);
        }

        public static Result Weave(SaveRoot s, ConfigSnapshot c, WeaveInput input)
        {
            var yarn = Find.Yarn(s, input.yarnId);
            if (yarn == null) return Result.Fail("没有选纱线，织机不会自动取纱（docs/06 §4）");
            var v = c.varieties.Find(x => x.id == input.varietyId);
            if (v == null || !v.launch || v.liningOnly) return Result.Fail("这台织机织不了这个品种");
            if (!Unlocks.VarietyOpen(s, c, v.id)) return Result.Fail("这个品种还没解锁");
            if (input.segments.Count != c.balance.weave.segments) return Result.Fail("需要织完全部 " + c.balance.weave.segments + " 段");
            int hours = c.balance.day.HoursOf(WeaveHoursKey(v, input.patternId));
            if (!Progress.CanSpend(s, c, hours)) return Result.Fail("今天的工时不够织一匹（需要 " + hours + "）");

            double len = Math.Min(c.balance.day.boltLength, yarn.length * c.balance.day.clothPerYarnMeter);
            var bolt = new Bolt
            {
                id = Ids.Next(s, "bolt"),
                variety = v.id,
                dynastyStyle = FirstLaunchDynasty(c),
                yarnId = yarn.id,
                materialScore = yarn.materialScore,
                yarnScore = yarn.processScore,
                length = len,
                width = c.balance.day.widthDefault,
                weaveScore = (int)WeaveScore(input, yarn, v, c),
                patternId = string.IsNullOrEmpty(input.patternId) ? PatternPlain : input.patternId,
            };
            yarn.length -= len / c.balance.day.clothPerYarnMeter;
            if (yarn.length <= 1e-6) s.yarns.Remove(yarn);
            s.bolts.Add(bolt);
            Progress.Spend(s, hours);
            Progress.AwardProcessXp(s, c, hours, ItemQuality.IsDefect(bolt, c));
            if (bolt.variety == "juan") CompleteQuest(s, Tutorial1);
            return Result.Ok(bolt.id);
        }

        public static string FirstLaunchDynasty(ConfigSnapshot c)
        {
            var d = c.balance.quality.dynasties.Find(x => x.launch && x.parsed);
            return d != null ? d.dynasty : null;
        }

        // ---------------- 染 ----------------

        public const string TempCold = "cold";
        public const string TempWarm = "warm";
        public const string TempHot = "hot";

        static string TempLabel(string key) => key == TempCold ? "冷" : key == TempHot ? "热" : "温";

        public class DyeInput
        {
            public string boltId;
            public string dyeId;
            public string concentration = DyeCalc.Medium;
            public string temperature = TempWarm;
            /// <summary>入缸到起布的秒数。</summary>
            public double liftSeconds;
            public int stirSteady, stirOff, stirTotal;
            /// <summary>用当季新鲜染料（浓度系数加成，docs/04 §5）。</summary>
            public bool fresh;
        }

        public class DyePreview
        {
            public double tempMatch, timeMatch, stirStability, strength, score, uneven;
            public bool failureMottle;
        }

        /// <summary>操作换成吻合度（docs/04 §5 染缸操作表）。</summary>
        public static DyePreview PreviewDye(DyeInput input, ConfigSnapshot c)
        {
            var d = c.balance.dye;
            var p = new DyePreview();
            p.tempMatch = TempLabel(input.temperature) == d.bestTemp ? d.tempMatchBest : d.tempMatchOther;
            double center = d.LiftCenterOf(input.concentration);
            double off = Math.Max(0, Math.Abs(input.liftSeconds - center) - d.liftBandWidth * 0.5);
            p.timeMatch = Math.Max(d.liftMatchMin, 1 - off * d.liftPenaltyPerSecond);
            p.stirStability = input.stirTotal <= 0 ? 0 : Math.Min(1, (input.stirSteady + input.stirOff * d.stirOffWeight) / input.stirTotal);
            p.strength = DyeCalc.Strength(input.concentration, p.timeMatch, p.tempMatch, d);
            if (input.fresh) // 新鲜染料浓度系数 × 加成，强度仍封顶
                p.strength = Math.Min(d.strengthCap, p.strength * d.freshConcentrationFactor);
            p.score = DyeCalc.Score(p.tempMatch, p.timeMatch, p.stirStability, d);
            p.uneven = 1 - p.stirStability;
            p.failureMottle = p.uneven > d.unevenAllowedMax;
            return p;
        }

        /// <summary>起布确认：记一层、扣干染料、耗工时（docs/04 §2、§5）。未确认的取料与搅拌不进这里。</summary>
        public static Result Dye(SaveRoot s, ConfigSnapshot c, DyeInput input)
        {
            var bolt = Find.Bolt(s, input.boltId);
            if (bolt == null) return Result.Fail("没有这匹布");
            var dyeRow = c.dyes.Find(x => x.id == input.dyeId);
            if (dyeRow == null || !dyeRow.launch) return Result.Fail("这口缸不用这种染料");
            if (!Unlocks.DyeOpen(s, c, dyeRow.id)) return Result.Fail("这种染料还没解锁");
            var stock = Find.Dye(s, input.dyeId, input.fresh);
            int cost = c.balance.dye.costPerBolt;
            if (stock == null || stock.count < cost) return Result.Fail("侧架上没有足够的" + (input.fresh ? "鲜" : "干") + dyeRow.name);
            int hours = c.balance.day.HoursOf("dyeBath");
            if (!Progress.CanSpend(s, c, hours)) return Result.Fail("今天的工时不够浸染一匹（需要 " + hours + "）");

            var pv = PreviewDye(input, c);
            var layer = new DyeLayer
            {
                dyeId = dyeRow.id,
                concentration = input.concentration,
                strength = pv.strength,
                uneven = pv.uneven,
                score = Math.Round(pv.score, 1),
                maskId = pv.failureMottle ? "mask_" + bolt.id + "_" + (bolt.dyeLayers.Count + 1) : null,
            };
            var added = DyeCalc.AddLayer(bolt.dyeLayers, layer, c.balance.dye);
            bolt.dyeLayers = added.layers;
            bolt.dyePenalty += added.scorePenalty;
            stock.count -= cost;
            if (stock.count <= 0) s.dyes.Remove(stock);
            Progress.Spend(s, hours);
            Progress.AwardProcessXp(s, c, hours, ItemQuality.IsDefect(bolt, c));
            var r = Result.Ok(bolt.id);
            if (added.removed != null) r.notes.Add("第 " + (c.balance.dye.maxLayers + 1) + " 层挤掉了最早的一层");
            if (pv.failureMottle) r.notes.Add("色花：搅拌不稳，布上留下掩膜");
            return r;
        }

        // ---------------- 裁 ----------------

        public class CutInput
        {
            public string boltId;
            public string patternId = RuQun;
            public string slot;
            public bool rotated90;
            public bool seamOnFront;
            public string sizeClass = "standard";
        }

        /// <summary>只给界面用的提醒：横放、拼缝在正面（docs/12 §6，图标加文字由界面负责）。</summary>
        public static List<string> CutWarnings(CutInput input)
        {
            var w = new List<string>();
            if (input.rotated90) w.Add("布横放了：纹样方向转了 90 度");
            if (input.seamOnFront) w.Add("拼缝落在衣身正面");
            return w;
        }

        public static Result Cut(SaveRoot s, ConfigSnapshot c, CutInput input)
        {
            var bolt = Find.Bolt(s, input.boltId);
            if (bolt == null) return Result.Fail("没有这匹布");
            var pattern = c.patterns.Find(x => x.id == input.patternId);
            if (pattern == null || !pattern.launch) return Result.Fail("没有这个形制");
            if (!Unlocks.PatternOpen(s, c, pattern.id)) return Result.Fail("这个形制还没解锁");
            if (!pattern.parts.Contains(input.slot)) return Result.Fail("这个形制没有这个部件");
            var len = pattern.partLengths.Find(x => x.slot == input.slot);
            if (len == null) return Result.Fail("docs/04 §2 没有这个部件的用量");
            if (bolt.length + 1e-6 < len.length) return Result.Fail("布不够：需要 " + len.length + " 米，剩 " + bolt.length + " 米");
            var variety = c.varieties.Find(v => v.id == bolt.variety);
            if (variety != null && variety.liningOnly && input.slot != "inner") return Result.Fail("苎麻只作里层");
            int hours = c.balance.day.HoursOf("cutPart");
            if (!Progress.CanSpend(s, c, hours)) return Result.Fail("今天的工时不够裁一个部件（需要 " + hours + "）");

            var p = c.balance.penalties;
            double score = c.balance.weave.cutStartScore;
            bool patterned = !string.IsNullOrEmpty(bolt.patternId) && bolt.patternId != PatternPlain;
            if (input.rotated90 && patterned) score += p.orientation90;
            if (input.seamOnFront) score += p.seamFront;

            var piece = new Piece
            {
                id = Ids.Next(s, "piece"),
                pattern = pattern.id,
                slot = input.slot,
                boltId = bolt.id,
                lengthUsed = len.length,
                sizeClass = input.sizeClass,
                cutScore = (int)Math.Max(0, Math.Round(score, MidpointRounding.AwayFromZero)),
                sewScore = null,
            };
            bolt.length = Math.Round(bolt.length - len.length, 3);
            s.pieces.Add(piece);
            Progress.Spend(s, hours);
            Progress.AwardProcessXp(s, c, hours, ItemQuality.IsDefect(bolt, c));
            var r = Result.Ok(piece.id);
            r.notes.AddRange(CutWarnings(input));
            return r;
        }

        // ---------------- 缝 ----------------

        public static Result Sew(SaveRoot s, ConfigSnapshot c, string pieceId, IList<Beat> needles)
        {
            var piece = Find.Piece(s, pieceId);
            if (piece == null) return Result.Fail("没有这片衣片");
            if (piece.sewScore.HasValue) return Result.Fail("这片已经缝过了");
            if (needles == null || needles.Count != c.balance.weave.sewNeedlesPerPart)
                return Result.Fail("沿缝需要 " + c.balance.weave.sewNeedlesPerPart + " 针");
            int hours = c.balance.day.HoursOf("sewPart");
            if (!Progress.CanSpend(s, c, hours)) return Result.Fail("今天的工时不够缝一个部件（需要 " + hours + "）");

            double score = Beats.Average(needles, c.balance.weave);
            // 层序先里后外（docs/07 §4）：还有没缝的里层时先缝外层，记一次缝序错
            if (ItemQuality.LayerOf(piece.slot) != "inner" && s.pieces.Exists(x => x.slot == "inner" && !x.sewScore.HasValue))
                score += c.balance.penalties.sewOrder;
            piece.sewScore = (int)Math.Max(0, Math.Round(score, MidpointRounding.AwayFromZero));
            Progress.Spend(s, hours);
            var bolt = Find.Bolt(s, piece.boltId);
            Progress.AwardProcessXp(s, c, hours, bolt != null && ItemQuality.IsDefect(bolt, c));
            return Result.Ok(piece.id);
        }

        // ---------------- 人台 ----------------

        public class AssembleInput
        {
            public string patternId = RuQun;
            public List<string> pieceIds = new List<string>();
            /// <summary>人台是否打开过；没打开就入库要扣缝制分（docs/04 §5）。</summary>
            public bool previewSeen = true;
            /// <summary>为空时用形制名。</summary>
            public string name;
        }

        public static Result Assemble(SaveRoot s, ConfigSnapshot c, AssembleInput input)
        {
            var pattern = c.patterns.Find(x => x.id == input.patternId);
            if (pattern == null) return Result.Fail("没有这个形制");
            var pieces = new List<Piece>();
            foreach (var id in input.pieceIds)
            {
                var p = Find.Piece(s, id);
                if (p == null) return Result.Fail("衣片不在侧架上");
                if (!p.sewScore.HasValue) return Result.Fail("还有部件没缝好（" + p.slot + "）");
                if ((p.pattern ?? RuQun) != pattern.id) return Result.Fail("衣片不是这个形制裁的");
                pieces.Add(p);
            }
            foreach (var slot in pattern.parts)
                if (!pattern.optionalParts.Contains(slot) && !pieces.Exists(p => p.slot == slot))
                    return Result.Fail("每个可见部件都要有布（docs/07 §6），还缺 " + slot);

            var g = new Garment
            {
                id = Ids.Next(s, "garment"),
                name = string.IsNullOrEmpty(input.name) ? pattern.name : input.name,
                pattern = pattern.id,
                dynastyStyle = pattern.dynasty,
                previewSeen = input.previewSeen,
            };
            int formPenalty = input.previewSeen ? 0 : c.balance.penalties.formSkipped;
            bool extreme = false;
            foreach (var p in pieces)
            {
                g.parts.Add(new GarmentPart
                {
                    slot = p.slot,
                    boltId = p.boltId,
                    lengthUsed = p.lengthUsed,
                    cutScore = p.cutScore,
                    sewScore = Math.Max(0, p.sewScore.Value + formPenalty),
                });
                if (p.sizeClass != "standard") extreme = true;
            }

            if (ItemQuality.TryGarmentQ(s, c, g.parts, out var q, out var outer))
            {
                g.scores = outer;
                g.tier = QualityCalc.TierOf(q, c.balance.quality);
                g.traits = ItemQuality.Traits(s, c, g, outer, pattern, extreme);
            }
            else
            {
                g.scores = outer ?? new GarmentScores();
                g.tier = null; // 待评定：分数不全时不猜（构架文档 §4.5）
            }

            foreach (var p in pieces) s.pieces.Remove(p);
            s.garments.Add(g);

            // 成衣入库：按成衣档为部件的裁、缝工时补足经验差额（docs/04 §7）
            if (g.tier != null)
            {
                var e = c.balance.economy;
                double diff = Progress.TierFactor(g.tier, e) - e.xpCommon;
                if (diff > 0)
                {
                    int hours = g.parts.Count * (c.balance.day.HoursOf("cutPart") + c.balance.day.HoursOf("sewPart"));
                    Progress.AddXp(s, c, (int)Math.Round(hours * e.xpPerHour * diff, MidpointRounding.AwayFromZero));
                }
            }

            var outerBolt = g.parts.Count > 0 ? Find.Bolt(s, g.parts.Find(p => ItemQuality.LayerOf(p.slot) == "outer")?.boltId ?? g.parts[0].boltId) : null;
            if (g.pattern == RuQun && outerBolt != null && outerBolt.dyeLayers.Exists(l => l.dyeId == "indigo"))
                CompleteQuest(s, Tutorial2);
            return Result.Ok(g.id);
        }

        // ---------------- 拆衣 ----------------

        /// <summary>拆衣：布回仓库，染色保留，边损加一（docs/02 §3、docs/04 §5）；在柜里的下架（docs/09 §2）。</summary>
        public static Result Unpick(SaveRoot s, ConfigSnapshot c, string garmentId)
        {
            var g = Find.Garment(s, garmentId);
            if (g == null) return Result.Fail("没有这件成衣");
            var touched = new HashSet<string>();
            foreach (var part in g.parts)
            {
                var bolt = Find.Bolt(s, part.boltId);
                if (bolt == null) continue;
                bolt.length = Math.Round(bolt.length + part.lengthUsed, 3);
                // 边损按一次拆衣计一次，不按部件数重复计
                if (touched.Add(bolt.id)) bolt.edgeDamage += c.balance.penalties.unpickEdgeDamage;
            }
            s.exhibit.slots.RemoveAll(x => x.itemKind == "garment" && x.itemId == g.id);
            s.garments.Remove(g);
            return Result.Ok();
        }

        public static void CompleteQuest(SaveRoot s, string id)
        {
            var q = Find.Quest(s, id);
            if (q != null) q.done = true;
        }
    }
}
