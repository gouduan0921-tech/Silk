using System.Collections.Generic;
using UnityEngine;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Save;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 裁桌（docs/07 §3）：上襦、裙、可选披帛与衬里，用量见 docs/04 §2。
    /// 横放有方向提醒，拼缝不妥给出图标加文字；警告不阻止确认，衣片仍保留。
    /// </summary>
    public class CutStation : StationBase
    {
        Renderer spread;
        Transform pieceOutline;
        string boltId;
        string slot = "upper";
        bool rotated, seamFront;
        string sizeClass = "standard";
        static readonly string[] Sizes = { "narrow", "standard", "wide" };

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "Table", new Vector3(0, 0.78f, 0), new Vector3(2.2f, 0.06f, 1.2f), Props.Wood);
            foreach (var x in new[] { -1f, 1f }) foreach (var z in new[] { -0.5f, 0.5f })
                Props.Box(root, "Leg", new Vector3(x, 0.38f, z), new Vector3(0.07f, 0.76f, 0.07f), Props.Wood);
            spread = Props.Cloth(root, "SpreadCloth", new Vector3(0, 0.815f, 0), new Vector3(90, 0, 0), new Vector2(1.9f, 0.5f), Props.Warp);
            pieceOutline = Props.Cloth(root, "PieceOutline", new Vector3(-0.3f, 0.82f, 0), new Vector3(90, 0, 0), new Vector2(0.6f, 0.4f), new Color(0.24f, 0.35f, 0.39f)).transform;
            Props.Box(root, "Scissors", new Vector3(0.7f, 0.83f, 0.35f), new Vector3(0.2f, 0.015f, 0.05f), new Color(0.2f, 0.2f, 0.2f));
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.6f, 0); col.size = new Vector3(2.4f, 1.3f, 1.4f);
        }

        public override bool Done => S.pieces.Count > 0 || S.garments.Count > 0;

        public override void OnEnter()
        {
            base.OnEnter();
            if (boltId == null || Play.Find.Bolt(S, boltId) == null)
            {
                var dyed = S.bolts.Find(b => b.dyeLayers.Count > 0);
                boltId = dyed != null ? dyed.id : (S.bolts.Count > 0 ? S.bolts[0].id : null);
            }
        }

        protected override void Update()
        {
            var b = boltId != null ? Play.Find.Bolt(S, boltId) : null;
            if (spread != null)
            {
                spread.gameObject.SetActive(b != null);
                if (b != null)
                {
                    Props.SetColor(spread, Names.BoltColor(C, b));
                    spread.transform.localScale = new Vector3(Mathf.Clamp((float)b.length / 4f, 0.2f, 2f), 0.5f, 1f);
                }
            }
            if (pieceOutline != null) pieceOutline.localEulerAngles = new Vector3(90, rotated ? 90 : 0, 0);
        }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Bolt || k == RackView.Kind.Piece, boltId,
                (k, id) => { if (k == RackView.Kind.Bolt) { boltId = id; Refresh(); } }, (k, id) => k == RackView.Kind.Bolt));
            var pattern = C.patterns.Find(p => p.id == "ruQun");
            var bolt = boltId != null ? Play.Find.Bolt(S, boltId) : null;
            var m = Plaque(bolt == null ? "从侧架拖一匹布到裁桌" : "铺布，裁下" + Names.Slot(slot));

            var parts = new List<string>(pattern.parts);
            var pg = new OptionGroup { label = "部件" };
            foreach (var p in parts)
            {
                var len = pattern.partLengths.Find(x => x.slot == p);
                pg.choices.Add(Names.Slot(p) + (len != null ? " " + Names.Meters(len.length) : ""));
            }
            pg.selected = parts.IndexOf(slot);
            pg.onSelect = i => slot = parts[i];
            m.options.Add(pg);
            m.options.Add(new OptionGroup { label = "铺布方向", choices = new List<string> { "顺经", "横放" }, selected = rotated ? 1 : 0, onSelect = i => rotated = i == 1 });
            m.options.Add(new OptionGroup { label = "拼缝位置", choices = new List<string> { "侧边", "正面" }, selected = seamFront ? 1 : 0, onSelect = i => seamFront = i == 1 });
            m.options.Add(new OptionGroup { label = "衣长与袖宽", choices = new List<string> { "窄", "标准", "宽" }, selected = System.Array.IndexOf(Sizes, sizeClass), onSelect = i => sizeClass = Sizes[i] });
            m.warnings.AddRange(Craft.CutWarnings(new Craft.CutInput { rotated90 = rotated, seamOnFront = seamFront }));
            if (sizeClass != "standard") m.warnings.Add("选了极端档：仍可做，但拿不到「合制」");

            int hours = C.balance.day.HoursOf("cutPart");
            var lenRow = pattern.partLengths.Find(x => x.slot == slot);
            bool enough = bolt != null && lenRow != null && bolt.length + 1e-6 >= lenRow.length;
            m.primaryLabel = "裁下" + Names.Slot(slot) + "（" + hours + " 工时）";
            m.primaryEnabled = enough && Progress.CanSpend(S, C, hours);
            m.body = bolt == null ? "" : !enough ? "这匹布剩 " + Names.Meters(bolt.length) + "，不够裁" + Names.Slot(slot) + "。"
                : !Progress.CanSpend(S, C, hours) ? "今天的工时不够。" : Names.Bolt(C, bolt) + "：" + Names.Layers(C, bolt) + "，剩 " + Names.Meters(bolt.length) + "。";
            m.onPrimary = () =>
            {
                var input = new Craft.CutInput { boltId = boltId, slot = slot, rotated90 = rotated, seamOnFront = seamFront, sizeClass = sizeClass };
                if (Run((s, c) => Craft.Cut(s, c, input), Names.Slot(slot) + "已裁下，留在侧架"))
                {
                    var left = parts.FindAll(p => !S.pieces.Exists(x => x.slot == p));
                    if (left.Count > 0) slot = left[0];
                    Refresh();
                }
            };
            if (S.pieces.Count > 0)
                m.secondary.Add(new KeyValuePair<string, System.Action>("走去针线", () => Workshop.Approach(Workshop.stations.Find(x => x.stationId == "station_sew"))));
            Show(m);
        }
    }
}
