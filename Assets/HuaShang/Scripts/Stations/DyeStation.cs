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
    /// 染缸（docs/07 §2）：侧架取染料，选淡/中/浓与水温，入缸后按拍搅拌，起布确认。
    /// 未起布就退出不扣料、不记层；起布后记一层并扣料；再进来不重复扣（构架文档 §3.2）。
    /// 染缸特写可以有液面；离开染缸后颜色在材质实例上（docs/03 §1）。
    /// </summary>
    public class DyeStation : StationBase
    {
        Renderer liquid, hangingCloth;
        string boltId, dyeId;
        bool freshDye;
        string concentration = DyeCalc.Medium;
        string temperature = Craft.TempWarm;
        float immersedAt = -1;
        BeatTrack stir;
        string resultBoltId;

        static readonly string[] ConcKeys = { DyeCalc.Light, DyeCalc.Medium, DyeCalc.Strong };
        static readonly string[] TempKeys = { Craft.TempCold, Craft.TempWarm, Craft.TempHot };

        public void BuildProps()
        {
            var root = transform;
            Props.Cyl(root, "Vat", new Vector3(0, 0.42f, 0), new Vector3(1.3f, 0.42f, 1.3f), Props.Pottery);
            var l = Props.Cyl(root, "Liquid", new Vector3(0, 0.845f, 0) /* 略高于缸口，避免与缸顶共面闪烁 */, new Vector3(1.18f, 0.01f, 1.18f), new Color(0.2f, 0.24f, 0.28f));
            l.GetComponent<MeshRenderer>().sharedMaterial = new Material(Props.Mat(Color.black)) { color = new Color(0.2f, 0.24f, 0.28f) };
            l.GetComponent<MeshRenderer>().sharedMaterial.SetFloat("_Smoothness", 0.85f);
            liquid = l.GetComponent<Renderer>();
            Props.Box(root, "PoleL", new Vector3(-1.1f, 0.9f, 0.6f), new Vector3(0.06f, 1.8f, 0.06f), Props.Wood);
            Props.Box(root, "PoleR", new Vector3(1.1f, 0.9f, 0.6f), new Vector3(0.06f, 1.8f, 0.06f), Props.Wood);
            Props.Cyl(root, "DryingRod", new Vector3(0, 1.78f, 0.6f), new Vector3(0.05f, 1.15f, 0.05f), Props.Wood).transform.localEulerAngles = new Vector3(0, 0, 90);
            hangingCloth = Props.Cloth(root, "HangingCloth", new Vector3(0, 1.22f, 0.6f), Vector3.zero, new Vector2(1.0f, 1.1f), Props.Warp);
            Props.Box(root, "DyeShelf", new Vector3(1.6f, 0.45f, -0.2f), new Vector3(0.6f, 0.9f, 0.4f), Props.Wood);
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.9f, 0.2f); col.size = new Vector3(2.6f, 1.9f, 1.8f);
        }

        public override bool Done
        {
            get { var b = Play.Find.Bolt(S, NewGameFactory.OpeningBoltId); return b != null && b.dyeLayers.Count > 0 || S.garments.Count > 0; }
        }

        public override void OnEnter()
        {
            base.OnEnter();
            immersedAt = -1; stir = null; dyeId = null; resultBoltId = null;
            if (boltId == null || Play.Find.Bolt(S, boltId) == null)
                boltId = S.bolts.Count > 0 ? S.bolts[0].id : null;
        }

        public override bool Back()
        {
            if (immersedAt >= 0) { immersedAt = -1; stir = null; depth = 0; Refresh(); return true; } // 未起布：不扣料、不记层
            if (depth == 2) { depth = 0; Refresh(); return true; }
            return base.Back();
        }

        public override void BeatKey() { stir?.Press(); }

        protected override void Update()
        {
            stir?.Tick();
            var b = boltId != null ? Play.Find.Bolt(S, boltId) : null;
            if (hangingCloth != null && b != null) Props.SetColor(hangingCloth, Names.BoltColor(C, b));
            if (liquid != null)
            {
                var c = dyeId != null ? Names.DyeColor(C, dyeId) * 0.6f : new Color(0.2f, 0.24f, 0.28f);
                c.a = 1f;
                Props.SetColor(liquid, c);
            }
        }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Bolt || k == RackView.Kind.Dye, dyeId != null ? RackView.DyeKey(dyeId, freshDye) : boltId,
                (k, id) =>
                {
                    if (immersedAt >= 0) return;
                    if (k == RackView.Kind.Bolt) boltId = id;
                    if (k == RackView.Kind.Dye)
                    {
                        string raw = RackView.DyeIdOf(id, out bool fresh);
                        if (C.dyes.Find(d => d.id == raw)?.role == "color") { dyeId = raw; freshDye = fresh; }
                    }
                    Refresh();
                }));

            if (depth == 2)
            {
                var b = Play.Find.Bolt(S, resultBoltId);
                var m = Plaque(Names.Layers(C, b) + "已染上", "完成");
                var layer = b.dyeLayers[b.dyeLayers.Count - 1];
                var q = ItemQuality.BoltQ(b, C);
                m.resultTier = Names.Tier(q.HasValue ? QualityCalc.TierOf(q.Value, C.balance.quality) : null);
                m.source = Names.Bolt(C, b) + "，" + Names.Layers(C, b) + "，出缸烘干";
                m.details = "染色分 " + layer.score.ToString("0") + "\n强度 " + layer.strength.ToString("0.00") + "\n不均 " + layer.uneven.ToString("0.00");
                if (layer.maskId != null) m.warnings.Add("色花：搅拌不稳，布上留下掩膜；布仍在，可作里层");
                m.primaryLabel = "走去裁桌";
                m.onPrimary = () => Workshop.Approach(Workshop.stations.Find(x => x.stationId == "station_cut"));
                Show(m);
                return;
            }

            if (immersedAt >= 0)
            {
                var m = Plaque("搅拌，到提示带起布");
                m.beat = stir.View;
                double center = C.balance.dye.LiftCenterOf(concentration);
                double half = C.balance.dye.liftBandWidth * 0.5;
                float elapsed = Time.time - immersedAt;
                m.body = "入缸后第 " + (center - half).ToString("0") + "–" + (center + half).ToString("0") + " 秒起布最合适。";
                m.primaryLabel = "起布";
                m.onPrimary = Lift;
                Show(m);
                return;
            }

            var bolt = boltId != null ? Play.Find.Bolt(S, boltId) : null;
            var mm = Plaque(dyeId == null ? "从侧架取一包染料" : "选好浓淡，再浸布");
            var conc = new OptionGroup { label = "这一缸的浓淡", choices = new List<string> { "淡", "中", "浓" }, selected = System.Array.IndexOf(ConcKeys, concentration) };
            conc.onSelect = i => concentration = ConcKeys[i];
            mm.options.Add(conc);
            var temp = new OptionGroup { label = "水温", choices = new List<string> { "冷", "温", "热" }, selected = System.Array.IndexOf(TempKeys, temperature) };
            temp.onSelect = i => temperature = TempKeys[i];
            mm.options.Add(temp);
            int hours = C.balance.day.HoursOf("dyeBath");
            bool hasStock = dyeId != null && (Play.Find.Dye(S, dyeId, freshDye)?.count ?? 0) >= C.balance.dye.costPerBolt;
            mm.primaryLabel = "将" + (bolt != null ? Names.Variety(C, bolt.variety) : "布") + "浸入缸中";
            mm.primaryEnabled = bolt != null && hasStock && Progress.CanSpend(S, C, hours);
            mm.body = bolt == null ? "侧架上没有布。" : !Progress.CanSpend(S, C, hours) ? "今天的工时不够浸染一匹（需要 " + hours + "）。"
                : "取料、浓淡与搅拌在起布前都不改库存。起布记一层，扣 " + C.balance.dye.costPerBolt + " 份染料，耗 " + hours + " 工时。";
            mm.onPrimary = () =>
            {
                immersedAt = Time.time;
                stir = new BeatTrack(C.balance.weave, BeatTrack.OpenEnded, "搅拌");
                stir.onBeat = b => Sfx.Play(b == Beat.Chaos ? Sfx.Cue.WaterBroken : Sfx.Cue.Water);
                depth = 1;
                Refresh();
            };
            Show(mm);
        }

        void Lift()
        {
            stir.Tick(); // 起布前错过的拍记乱
            int steady = 0, off = 0;
            foreach (var r in stir.results) { if (r == Beat.Steady) steady++; else if (r == Beat.Off) off++; }
            var input = new Craft.DyeInput
            {
                boltId = boltId, dyeId = dyeId, fresh = freshDye, concentration = concentration, temperature = temperature,
                liftSeconds = Time.time - immersedAt, stirSteady = steady, stirOff = off, stirTotal = stir.Total,
            };
            immersedAt = -1;
            stir = null;
            if (Run((s, c) => Craft.Dye(s, c, input))) { resultBoltId = boltId; depth = 2; dyeId = null; }
            else depth = 0;
            Refresh();
        }
    }
}
