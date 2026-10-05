using System.Collections.Generic;
using UnityEngine;
using HuaShang.Game;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Save;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 蚕房（docs/06）：收蚁、喂叶、控温、上蔟，每步至少隔一天；缫丝按拍抽丝，乱拍记断头。
    /// 养蚕的日结在工坊日结束时离屏发生（docs/19 §3）。首日不必进入。
    /// </summary>
    public class SilkStation : StationBase
    {
        readonly List<GameObject> silkworms = new List<GameObject>();
        Transform trayRoot;
        BeatTrack track;
        string basketId;
        string fineness = "fine";
        bool overfeed, crowded;
        string pending; // temper / reel

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "Shelf", new Vector3(0, 0.45f, 0), new Vector3(1.6f, 0.9f, 0.9f), Props.Wood);
            trayRoot = Props.Box(root, "Tray", new Vector3(0, 0.93f, 0), new Vector3(1.3f, 0.04f, 0.75f), new Color(0.62f, 0.55f, 0.42f)).transform;
            Props.Cyl(root, "ReelBasin", new Vector3(1.4f, 0.5f, -0.3f), new Vector3(0.6f, 0.2f, 0.6f), Props.Pottery);
            Props.Box(root, "Leaves", new Vector3(-1.2f, 0.9f, 0.2f), new Vector3(0.4f, 0.05f, 0.3f), new Color(0.35f, 0.5f, 0.3f));
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0.3f, 0.6f, 0); col.size = new Vector3(2.6f, 1.3f, 1.2f);
        }

        void SyncTray()
        {
            foreach (var g in silkworms) if (g) Destroy(g);
            silkworms.Clear();
            string st = S.tray.stage;
            int n = st == TrayStage.Empty ? 0 : 12;
            for (int i = 0; i < n; i++)
            {
                var p = new Vector3(-0.5f + (i % 6) * 0.2f, 0.97f, -0.2f + (i / 6) * 0.35f);
                bool cocoon = st == TrayStage.Mounted;
                var g = Props.Prim(PrimitiveType.Capsule, transform, cocoon ? "Cocoon" : "Silkworm", p,
                    cocoon ? new Vector3(0.05f, 0.035f, 0.05f) : new Vector3(0.025f, 0.05f, 0.025f), new Color(0.94f, 0.92f, 0.86f), false);
                if (!cocoon) g.transform.localEulerAngles = new Vector3(90, i * 25, 0);
                silkworms.Add(g);
            }
        }

        public override bool Done => S.yarns.Exists(y => y.id != NewGameFactory.OpeningYarnId) || S.tray.stage != TrayStage.Empty;

        public override void OnEnter() { base.OnEnter(); track = null; pending = null; SyncTray(); }

        public override bool Back()
        {
            if (track != null || depth == 2) { track = null; pending = null; depth = 0; Refresh(); return true; }
            return base.Back();
        }

        public override void BeatKey() { track?.Press(); }

        protected override void Update()
        {
            if (track == null) return;
            track.Tick();
            if (track.Done && depth == 1) { depth = 2; Refresh(); }
        }

        bool StepReady => !S.tray.lastStepDay.HasValue || S.dayIndex - S.tray.lastStepDay.Value >= 1;

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Cocoon || k == RackView.Kind.Yarn, basketId,
                (k, id) => { if (k == RackView.Kind.Cocoon) { basketId = id; Refresh(); } }, (k, id) => k == RackView.Kind.Cocoon));
            SyncTray();
            if (depth == 1 && track != null)
            {
                var m = Plaque(pending == "reel" ? "维持水温，抽丝" : "看温度针，在标记带内按下");
                m.beat = track.View;
                Show(m);
                return;
            }
            if (depth == 2 && track != null)
            {
                var results = new List<Beat>(track.results);
                if (pending == "temper")
                {
                    bool inBand = results.Count > 0 && results[0] != Beat.Chaos;
                    track = null; pending = null; depth = 0;
                    Run((s, c) => Silk.Temper(s, c, inBand), inBand ? "控温在带内" : "控温离开了标记带");
                    Refresh();
                    return;
                }
                var m = Plaque("接受这一束", "缫好了");
                int breaks = results.FindAll(b => b == Beat.Chaos).Count;
                m.details = "断头 " + breaks + " 次";
                m.primaryLabel = "接受";
                m.onPrimary = () =>
                {
                    track = null; pending = null; depth = 0;
                    Run((s, c) => Silk.Reel(s, c, basketId, fineness, results), "一束丝进了侧架");
                    if (Play.Find.Bolt(S, basketId) == null && !S.cocoons.Exists(x => x.id == basketId)) basketId = null;
                    Refresh();
                };
                Show(m);
                return;
            }

            var mm = Plaque(TrayTitle());
            int h = C.balance.day.HoursOf("feed");
            switch (S.tray.stage)
            {
                case TrayStage.Empty:
                    mm.body = "收蚁要买蚕种 " + C.balance.economy.seedPrice * C.balance.season.seedPerTray + " 丝钱。之后每步至少隔一个工坊日。";
                    mm.primaryLabel = "收蚁（" + h + " 工时）";
                    mm.onPrimary = () => { Run(Silk.Hatch, "蚕箔上有了一批蚁蚕"); Refresh(); };
                    break;
                case TrayStage.Hatched:
                    mm.options.Add(new OptionGroup { label = "喂叶", choices = new List<string> { "恰当", "过量" }, selected = overfeed ? 1 : 0, onSelect = i => overfeed = i == 1 });
                    mm.primaryLabel = "喂叶（" + h + " 工时）";
                    mm.primaryEnabled = StepReady;
                    mm.onPrimary = () => { Run((s, c) => Silk.Feed(s, c, overfeed), "喂过了"); Refresh(); };
                    break;
                case TrayStage.Fed:
                    mm.primaryLabel = "控温（" + h + " 工时）";
                    mm.primaryEnabled = StepReady;
                    mm.onPrimary = () => { pending = "temper"; track = new BeatTrack(C.balance.weave, 1, "温度针"); depth = 1; Refresh(); };
                    break;
                case TrayStage.Tempered:
                    mm.options.Add(new OptionGroup { label = "上蔟", choices = new List<string> { "适中", "过密" }, selected = crowded ? 1 : 0, onSelect = i => crowded = i == 1 });
                    mm.primaryLabel = "上蔟（" + h + " 工时）";
                    mm.primaryEnabled = StepReady;
                    mm.onPrimary = () => { Run((s, c) => Silk.Mount(s, c, crowded), "结茧了，茧篮在侧架"); Refresh(); };
                    break;
            }
            if (!StepReady && S.tray.stage != TrayStage.Empty) mm.body = "这一步要隔一个工坊日再做。";
            if (S.cocoons.Count > 0)
            {
                if (basketId == null || !S.cocoons.Exists(x => x.id == basketId)) basketId = S.cocoons[0].id;
                mm.options.Add(new OptionGroup
                {
                    label = "缫丝细度",
                    choices = new List<string> { "细", "中", "粗" },
                    selected = fineness == "fine" ? 0 : fineness == "medium" ? 1 : 2,
                    onSelect = i => fineness = i == 0 ? "fine" : i == 1 ? "medium" : "coarse",
                });
                int rh = C.balance.day.HoursOf("reel");
                mm.secondary.Add(new KeyValuePair<string, System.Action>("缫一束（" + rh + " 工时）", () =>
                {
                    if (!Progress.CanSpend(S, C, rh)) { H.Toast("今天的工时不够"); return; }
                    pending = "reel";
                    track = new BeatTrack(C.balance.weave, C.balance.season.reelBeats, "抽丝");
                    track.onBeat = b => Sfx.Play(b == Beat.Chaos ? Sfx.Cue.Break : Sfx.Cue.Water);
                    depth = 1; Refresh();
                }));
            }
            Show(mm);
        }

        string TrayTitle()
        {
            switch (S.tray.stage)
            {
                case TrayStage.Hatched: return "蚕箔：喂叶";
                case TrayStage.Fed: return "蚕箔：控温";
                case TrayStage.Tempered: return "蚕箔：上蔟";
                default: return "蚕箔是空的";
            }
        }
    }
}
