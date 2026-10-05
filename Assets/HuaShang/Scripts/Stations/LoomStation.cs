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
    /// 织机（docs/07 §1）：经线和梭是动画与节拍，不是布料粒子（docs/03 §1）。
    /// 教学第 1 步可以直接确认开局绢（docs/10 §2）：不新增布，不消耗细丝。
    /// </summary>
    public class LoomStation : StationBase
    {
        Transform shuttle;
        Renderer clothOnLoom;
        string yarnId;
        string varietyId;
        string patternId = Craft.PatternPlain;
        BeatTrack track;
        readonly List<List<Beat>> segments = new List<List<Beat>>();
        string lastBoltId;

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "LoomBase", new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.08f, 1.2f), Props.Wood);
            foreach (var x in new[] { -0.78f, 0.78f })
                foreach (var z in new[] { -0.55f, 0.55f })
                    Props.Box(root, "Post", new Vector3(x, 0.8f, z), new Vector3(0.07f, 1.6f, 0.07f), Props.Wood);
            Props.Box(root, "TopBeam", new Vector3(0, 1.6f, 0.55f), new Vector3(1.7f, 0.08f, 0.08f), Props.Wood);
            Props.Box(root, "BreastBeam", new Vector3(0, 0.9f, -0.55f), new Vector3(1.7f, 0.08f, 0.08f), Props.Wood);
            for (int i = 0; i < 36; i++)
            {
                float x = -0.6f + i * (1.2f / 35f);
                var w = Props.Box(root, "Warp", new Vector3(x, 1.25f, 0f), new Vector3(0.006f, 0.006f, 1.1f), Props.Warp);
                w.transform.localEulerAngles = new Vector3(-32f, 0, 0);
            }
            clothOnLoom = Props.Cloth(root, "WovenCloth", new Vector3(0, 0.93f, -0.42f), new Vector3(70, 0, 0), new Vector2(1.2f, 0.3f), Props.Warp);
            shuttle = Props.Box(root, "Shuttle", new Vector3(-0.7f, 1.0f, -0.25f), new Vector3(0.22f, 0.04f, 0.05f), Props.WoodLight).transform;
            Props.Box(root, "Bench", new Vector3(0, 0.25f, -1.1f), new Vector3(1.0f, 0.5f, 0.35f), Props.Wood);
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.8f, 0); col.size = new Vector3(2, 1.8f, 2);
        }

        public override bool Done => Play.Find.Quest(S, Craft.Tutorial1)?.done == true;

        public override void OnEnter()
        {
            base.OnEnter();
            track = null;
            segments.Clear();
        }

        public override bool Back()
        {
            if (track != null || depth >= 2) { track = null; segments.Clear(); depth = 0; Refresh(); return true; } // 未下机不改库存
            return base.Back();
        }

        public override void BeatKey()
        {
            if (track == null) return;
            track.Press();
        }

        protected override void Update()
        {
            if (track == null) return;
            track.Tick();
            if (shuttle != null) shuttle.localPosition = new Vector3(Mathf.Lerp(-0.7f, 0.7f, Mathf.PingPong(track.results.Count + track.View().cursor, 1f)), 1.0f, -0.25f);
            if (track.Done)
            {
                segments.Add(new List<Beat>(track.results));
                if (segments.Count < C.balance.weave.segments) StartSegment();
                else { track = null; depth = 2; Refresh(); }
            }
        }

        void StartSegment()
        {
            track = new BeatTrack(C.balance.weave, C.balance.weave.beatsPerSegment, "第 " + (segments.Count + 1) + " / " + C.balance.weave.segments + " 段：踏板与梭");
            track.onBeat = b => Sfx.Play(b == Beat.Steady ? Sfx.Cue.ShuttleSteady : Sfx.Cue.ShuttleRough);
            Refresh();
        }

        public override void Refresh()
        {
            var opening = Play.Find.Bolt(S, NewGameFactory.OpeningBoltId);
            bool tutorialPending = Play.Find.Quest(S, Craft.Tutorial1)?.done == false;
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Yarn || k == RackView.Kind.Bolt, yarnId,
                (k, id) => { if (k == RackView.Kind.Yarn) { yarnId = id; Refresh(); } }, (k, id) => k == RackView.Kind.Yarn && track == null));

            if (depth == 0 && tutorialPending && opening != null)
            {
                var m = Plaque("确认开局绢 A");
                m.body = Names.Bolt(C, opening) + "：" + Names.Fineness(C.opening.bolt.fineness) + "丝，" + Names.Layers(C, opening) + "，" + Names.Meters(opening.length)
                         + "。开局已有这匹绢，确认库存即完成教学第一步；不新增布，不消耗细丝 B。";
                m.primaryLabel = "确认开局绢";
                m.onPrimary = () => { if (Run((s, c) => Craft.ConfirmBolt(s, c, opening.id), "开局绢 A 已确认；细丝 B 仍未织。")) { lastBoltId = opening.id; depth = 3; Refresh(); } };
                m.secondary.Add(new KeyValuePair<string, System.Action>("改织一匹新布", () => { depth = 1; Refresh(); }));
                Show(m);
                return;
            }
            if (depth == 3)
            {
                var b = Play.Find.Bolt(S, lastBoltId);
                var m = Plaque(b != null ? Names.Bolt(C, b) + " 在侧架上" : "完成", "完成");
                if (b != null)
                {
                    var q = ItemQuality.BoltQ(b, C);
                    m.resultTier = Names.Tier(q.HasValue ? QualityCalc.TierOf(q.Value, C.balance.quality) : null);
                    m.source = Names.Season(S.season) + " · " + Names.Variety(C, b.variety) + "，" + Names.Meters(b.length) + "，幅宽 " + b.width;
                    m.details = "织造分 " + b.weaveScore + "\n原料分 " + b.materialScore + "\n成纱分 " + b.yarnScore;
                }
                m.primaryLabel = "走去染缸";
                m.onPrimary = () => Workshop.Approach(Workshop.stations.Find(x => x.stationId == "station_dye"));
                Show(m);
                return;
            }
            if (depth <= 1)
            {
                var open = Unlocks.OpenVarieties(S, C);
                if (varietyId == null || !open.Contains(varietyId)) varietyId = open.Count > 0 ? open[0] : null;
                var m = Plaque(yarnId == null ? "从侧架选一束纱" : "选品种与花本，再开织");
                var vg = new OptionGroup { label = "品种" };
                foreach (var v in open) vg.choices.Add(Names.Variety(C, v));
                vg.selected = open.IndexOf(varietyId);
                vg.onSelect = i => varietyId = open[i];
                m.options.Add(vg);
                var patterns = new List<string> { Craft.PatternPlain };
                if (S.level >= 2) patterns.Add(Craft.PatternGrid); // docs/10 §1：细方格花本 2 级
                var pg = new OptionGroup { label = "花本" };
                foreach (var p in patterns) pg.choices.Add(p == Craft.PatternPlain ? "素" : "细方格");
                pg.selected = patterns.IndexOf(patternId);
                pg.onSelect = i => patternId = patterns[i];
                m.options.Add(pg);
                int hours = C.balance.day.HoursOf("weavePlain");
                m.primaryLabel = "开织（" + hours + " 工时）";
                m.primaryEnabled = yarnId != null && Progress.CanSpend(S, C, hours);
                m.body = Progress.CanSpend(S, C, hours) ? "织机不会自动取纱，先在侧架点一束。" : "今天的工时不够织一匹。";
                m.onPrimary = () => { segments.Clear(); depth = 1; StartSegment(); };
                if (track != null)
                {
                    m = Plaque("踩踏板，投梭");
                    m.beat = track.View;
                    m.body = "节拍落在目标线上为稳。一段里乱太多，这段分数封顶。";
                }
                Show(m);
                return;
            }
            if (depth == 2)
            {
                var input = new Craft.WeaveInput { yarnId = yarnId, varietyId = varietyId, patternId = patternId, segments = segments };
                var m = Plaque("下机");
                var yarn = Play.Find.Yarn(S, yarnId);
                var vrow = C.varieties.Find(v => v.id == varietyId);
                if (yarn != null && vrow != null) m.details = "织造分（预计）" + Craft.WeaveScore(input, yarn, vrow, C);
                m.body = "接受这一匹，布进入侧架。";
                m.primaryLabel = "下机";
                m.onPrimary = () =>
                {
                    var r = G.Run((s, c) => Craft.Weave(s, c, input));
                    if (r.ok) { lastBoltId = r.createdId; yarnId = null; depth = 3; Refresh(); }
                    else H.Toast(r.error);
                };
                Show(m);
            }
        }
    }
}
