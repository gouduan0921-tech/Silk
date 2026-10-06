using System.Collections.Generic;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Save;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 针线（docs/07 §4）：沿缝推进，针距离开标记带则布声变干并扣缝制分。层序先里后外。
    /// 落针是操作反馈；缝制分按 docs/04 §5 的节拍分算，不是界面样板的三下落针。
    /// </summary>
    public class SewStation : StationBase
    {
        string pieceId;
        BeatTrack needles;
        Renderer pieceOnTable;
        Transform needle;

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "Table", new Vector3(0, 0.74f, 0), new Vector3(1.4f, 0.05f, 0.9f), Props.Wood);
            foreach (var x in new[] { -0.62f, 0.62f }) foreach (var z in new[] { -0.38f, 0.38f })
                Props.Box(root, "Leg", new Vector3(x, 0.36f, z), new Vector3(0.06f, 0.72f, 0.06f), Props.Wood);
            pieceOnTable = Props.Cloth(root, "Piece", new Vector3(0, 0.77f, 0), new Vector3(90, 0, 0), new Vector2(0.8f, 0.6f), Props.Warp);
            needle = Props.Box(root, "Needle", new Vector3(-0.35f, 0.8f, 0), new Vector3(0.12f, 0.004f, 0.004f), new Color(0.75f, 0.75f, 0.72f)).transform;
            Props.Cyl(root, "Spool", new Vector3(0.5f, 0.8f, 0.3f), new Vector3(0.06f, 0.04f, 0.06f), new Color(0.24f, 0.35f, 0.39f));
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0); col.size = new Vector3(1.6f, 1.2f, 1.1f);
        }

        public override IEnumerable<Renderer> LiveRenderers() { if (pieceOnTable != null) yield return pieceOnTable; if (needle != null) yield return needle.GetComponent<Renderer>(); }

        public override bool Done => S.pieces.Exists(p => p.sewScore.HasValue) || S.garments.Count > 0;

        public override void OnEnter()
        {
            base.OnEnter();
            needles = null;
            var unsewn = S.pieces.Find(p => !p.sewScore.HasValue && p.slot != "drape");
            pieceId = unsewn?.id;
        }

        public override bool Back()
        {
            if (needles != null || depth == 2) { needles = null; depth = 0; Refresh(); return true; } // 未接受的缝合不写分
            return base.Back();
        }

        public override void BeatKey() { needles?.Press(); }

        protected override void Update()
        {
            if (needles != null)
            {
                needles.Tick();
                if (needle != null) needle.localPosition = new Vector3(Mathf.Lerp(-0.35f, 0.35f, needles.results.Count / (float)needles.Total), 0.8f, 0);
                if (needles.Done && depth == 1) { depth = 2; Refresh(); }
            }
            var p = pieceId != null ? Play.Find.Piece(S, pieceId) : null;
            var b = p != null ? Play.Find.Bolt(S, p.boltId) : null;
            if (pieceOnTable != null)
            {
                pieceOnTable.gameObject.SetActive(p != null);
                if (b != null) Props.SetColor(pieceOnTable, Names.BoltColor(C, b));
            }
        }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Piece, pieceId,
                (k, id) => { if (needles == null) { pieceId = id; depth = 0; Refresh(); } },
                (k, id) => !(Play.Find.Piece(S, id)?.sewScore.HasValue ?? true)));

            var piece = pieceId != null ? Play.Find.Piece(S, pieceId) : null;
            if (depth == 1 && needles != null)
            {
                var m = Plaque("沿着缝线，让衣片接在一起");
                m.beat = needles.View;
                m.body = "针落在标记带内布声清；偏了布声变干。";
                Show(m);
                return;
            }
            if (depth == 2 && needles != null)
            {
                var m = Plaque("接受这一缝", "缝好了");
                m.details = "缝制分（预计）" + Beats.Average(needles.results, C.balance.weave).ToString("0");
                m.primaryLabel = "接受";
                var results = new List<Beat>(needles.results);
                m.onPrimary = () =>
                {
                    needles = null;
                    if (Run((s, c) => Craft.Sew(s, c, pieceId, results), Names.Slot(piece.slot, piece.pattern) + "缝好了"))
                    {
                        var next = S.pieces.Find(p => !p.sewScore.HasValue && p.slot != "drape");
                        pieceId = next?.id;
                        depth = next == null ? 3 : 0;
                    }
                    else depth = 0;
                    Refresh();
                };
                Show(m);
                return;
            }
            if (depth == 3 || (piece == null && S.pieces.Exists(p => p.sewScore.HasValue)))
            {
                ShowSewnSummary();
                return;
            }
            int hours = C.balance.day.HoursOf("sewPart");
            var mm = Plaque(piece == null ? "从侧架选一片衣片" : "沿缝：" + Names.Slot(piece.slot, piece.pattern));
            mm.primaryLabel = "落针（" + hours + " 工时）";
            mm.primaryEnabled = piece != null && !piece.sewScore.HasValue && Progress.CanSpend(S, C, hours);
            mm.body = piece == null ? "披帛可以留着不缝。" : !Progress.CanSpend(S, C, hours) ? "今天的工时不够。" : "按空格落针，共 " + C.balance.weave.sewNeedlesPerPart + " 针。";
            mm.onPrimary = () =>
            {
                needles = new BeatTrack(C.balance.weave, C.balance.weave.sewNeedlesPerPart, "落针");
                bool gauze = Play.Find.Bolt(S, piece.boltId)?.variety == "suSha";
                needles.onBeat = b => { Sfx.Play(b == Beat.Steady ? Sfx.Cue.Needle : Sfx.Cue.NeedleDry); Sfx.Play(gauze ? Sfx.Cue.ClothGauze : Sfx.Cue.ClothSilk); };
                depth = 1;
                Refresh();
            };
            Show(mm);
        }

        /// <summary>缝合完成牌：档位只显示 docs/04 公式的结果（入库时定档），最多 3 个词条，一句来源。</summary>
        void ShowSewnSummary()
        {
            var sewn = S.pieces.FindAll(p => p.sewScore.HasValue);
            var m = Plaque(string.Join("与", sewn.ConvertAll(p => Names.Slot(p.slot, p.pattern))) + "接好了", "缝合完成");
            var parts = sewn.ConvertAll(p => new GarmentPart { slot = p.slot, boltId = p.boltId, lengthUsed = p.lengthUsed, cutScore = p.cutScore, sewScore = p.sewScore });
            if (ItemQuality.TryGarmentQ(S, C, parts, out var q, out var outer))
            {
                m.resultTier = Names.Tier(QualityCalc.TierOf(q, C.balance.quality));
                m.body = "档位与词条在人台收成成衣时写入。";
                m.details = "原料 " + outer.material + "\n成纱 " + outer.yarn + "\n织造 " + outer.weave + "\n染色 " + (outer.dye.HasValue ? outer.dye.Value.ToString("0") : "未染")
                            + "\n裁剪 " + outer.cut?.ToString("0") + "\n缝制 " + outer.sew?.ToString("0");
            }
            var b = parts.Count > 0 ? Play.Find.Bolt(S, parts[0].boltId) : null;
            if (b != null) m.source = (b.id == NewGameFactory.OpeningBoltId ? "春日开局" : Names.Season(S.season) + "茧") + "平纹" + Names.Variety(C, b.variety) + "，" + Names.Layers(C, b) + "，" + PatternLabel(sewn);
            m.primaryLabel = "走近人台";
            m.onPrimary = () => Workshop.Approach(Workshop.stations.Find(x => x.stationId == "station_form"));
            Show(m);
        }

        /// <summary>来源句里的风格与形制，例如「汉风襦裙」「宋风大袖衫」（docs/12 §2 来源句）。</summary>
        string PatternLabel(List<Piece> sewn)
        {
            string pid = sewn.Count > 0 ? sewn[0].pattern ?? Craft.RuQun : Craft.RuQun;
            var row = C.patterns.Find(x => x.id == pid);
            if (row == null) return "";
            return Names.Dynasty(row.dynasty) + row.name;
        }
    }
}