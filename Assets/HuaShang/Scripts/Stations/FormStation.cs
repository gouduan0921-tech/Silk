using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Save;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 人台（docs/07 §5）：与舞台同一份布料配置，只用基础风与中性白光。可以转、藏外层、近看衣领和下摆。
    /// 上襦和裙收成成衣之后才能上场。「材料透光」对照：同色素纱透出格栅与叶片，绸遮住背景；不入库、不解锁绸。
    /// </summary>
    public class FormStation : StationBase
    {
        public GreyboxBody body;
        public GarmentVisual visual;
        public Transform collarPose, hemPose, transmittancePose, turntable;
        Transform transRig;
        bool outerHidden;
        string shownGarmentId;
        string resultGarmentId;
        bool transmittanceBuilt;

        public void BuildProps()
        {
            var root = transform;
            Props.Cyl(root, "Plinth", new Vector3(0, 0.05f, 0), new Vector3(0.8f, 0.05f, 0.8f), Props.Wood);
            turntable = new GameObject("Turntable").transform;
            turntable.SetParent(root, false);
            turntable.localPosition = new Vector3(0, 0.1f, 0);
            body = GreyboxBody.Create("SK_xiShi_Form", turntable, Props.Mat(new Color(0.82f, 0.8f, 0.76f)));
            visual = body.gameObject.AddComponent<GarmentVisual>();
            visual.body = body;
            visual.context = ClothContext.Form;
            visual.ownerId = "form";
            collarPose = Props.Pose(root, "CollarPose", new Vector3(0.15f, 1.6f, -0.9f), new Vector3(0, 1.48f, 0));
            hemPose = Props.Pose(root, "HemPose", new Vector3(0.2f, 0.45f, -1.3f), new Vector3(0, 0.3f, 0));

            transRig = new GameObject("材料透光对照").transform;
            transRig.SetParent(root, false);
            transRig.localPosition = new Vector3(2.4f, 0, 0.4f);
            for (int i = 0; i < 7; i++)
            {
                Props.Box(transRig, "LatticeV", new Vector3(-0.6f + i * 0.2f, 1.2f, 0.3f), new Vector3(0.03f, 1.4f, 0.03f), Props.Wood);
                Props.Box(transRig, "LatticeH", new Vector3(0, 0.55f + i * 0.2f, 0.3f), new Vector3(1.25f, 0.03f, 0.03f), Props.Wood);
            }
            for (int i = 0; i < 6; i++)
            {
                var leaf = Props.Prim(PrimitiveType.Sphere, transRig, "Leaf", new Vector3(-0.5f + i * 0.2f, 1.0f + (i % 2) * 0.35f, 0.36f), new Vector3(0.12f, 0.22f, 0.02f), new Color(0.32f, 0.45f, 0.28f), false);
                leaf.transform.localEulerAngles = new Vector3(0, 0, 25 * (i % 3 - 1));
            }
            Props.Cyl(transRig, "Rod", new Vector3(0, 1.85f, 0), new Vector3(0.03f, 0.7f, 0.03f), Props.Wood).transform.localEulerAngles = new Vector3(0, 0, 90);
            transmittancePose = Props.Pose(root, "TransmittancePose", new Vector3(2.4f, 1.3f, -1.6f), new Vector3(2.4f, 1.2f, 0.4f));

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.9f, 0); col.size = new Vector3(1.2f, 1.8f, 1.2f);
        }

        /// <summary>同色素纱与绸：只是对照，不读写存档。</summary>
        void BuildTransmittance()
        {
            if (transmittanceBuilt) return;
            transmittanceBuilt = true;
            var paint = GreyboxMeshes.PaintMap(0.05f);
            var fine = C.balance.quality.bands.Find(b => b.tier == QualityCalc.Fine);
            double t = QualityCalc.T((fine.qMin + fine.qMax) * 0.5, C.balance.quality);
            string dyn = Craft.FirstLaunchDynasty(C);
            int i = 0;
            foreach (var v in new[] { "suSha", "chou" })
            {
                var go = new GameObject("对照_" + Names.Variety(C, v));
                go.transform.SetParent(transRig, false);
                go.transform.localPosition = new Vector3(i == 0 ? -0.33f : 0.33f, 1.82f, 0);
                go.AddComponent<MeshFilter>().sharedMesh = GreyboxMeshes.Panel("P_compare", 0.55f, 1.25f);
                var part = go.AddComponent<ClothPart>();
                part.sourceRenderer = go.AddComponent<MeshRenderer>();
                part.context = ClothContext.Form;
                part.layer = ClothLayer.Outer;
                part.ownerId = "transmittance";
                var d = ClothDescribe.FromLayers(C, v, t, dyn, null, null, 0, null);
                part.Build(d, ClothLook.DyedColor(null, C.balance.dye), C.clothFixed, paint, null, false);
                i++;
            }
        }

        public override bool Done => S.garments.Count > 0;

        public override void OnEnter()
        {
            base.OnEnter();
            outerHidden = false;
            resultGarmentId = null;
            ShowCurrent();
        }

        public override void OnApproach() { ShowCurrent(); }

        /// <summary>人台上这一件的形制：按最早缝好的衣片（docs/21 §10 衣片带形制）。</summary>
        string CurrentPattern()
        {
            var first = S.pieces.Find(p => p.sewScore.HasValue);
            return first != null ? first.pattern ?? Craft.RuQun : Craft.RuQun;
        }

        List<Piece> SewnOfPattern(string pattern) => S.pieces.FindAll(p => p.sewScore.HasValue && (p.pattern ?? Craft.RuQun) == pattern);

        void ShowCurrent()
        {
            string patternNow = CurrentPattern();
            var sewn = SewnOfPattern(patternNow);
            if (sewn.Count > 0)
            {
                var prow = C.patterns.Find(x => x.id == patternNow);
                var specs = sewn.ConvertAll(p => new GarmentVisual.PartSpec { slot = p.slot, bolt = Play.Find.Bolt(S, p.boltId), sewScore = p.sewScore, pattern = patternNow, dynasty = prow?.dynasty });
                double t = 0;
                var parts = sewn.ConvertAll(p => new GarmentPart { slot = p.slot, boltId = p.boltId, cutScore = p.cutScore, sewScore = p.sewScore });
                if (ItemQuality.TryGarmentQ(S, C, parts, out var q, out _)) t = QualityCalc.T(q, C.balance.quality);
                visual.Build(C, specs, t, false);
                shownGarmentId = null;
                return;
            }
            var g = S.garments.Find(x => !S.exhibit.slots.Exists(e => e.itemId == x.id));
            if (g != null && g.id != shownGarmentId) { visual.BuildGarment(S, C, g); shownGarmentId = g.id; }
            else if (g == null) { visual.Clear(); shownGarmentId = null; }
        }

        public override bool Back()
        {
            if (depth == 1) { depth = 0; Workshop.cameraDirector.GoTo(enterPose, enterFov); Refresh(); return true; }
            if (depth == 2) { depth = 0; Refresh(); return true; }
            return base.Back();
        }

        protected override void Update()
        {
            if (Workshop == null || Workshop.current != this || Workshop.layer != WorkshopController.Layer.Inside) return;
            var kb = Keyboard.current;
            if (kb != null && turntable != null)
            {
                if (kb.aKey.isPressed) turntable.Rotate(0, 90 * Time.deltaTime, 0);
                if (kb.dKey.isPressed) turntable.Rotate(0, -90 * Time.deltaTime, 0);
            }
        }

        public void ShowTransmittance()
        {
            BuildTransmittance();
            depth = 1;
            Workshop.cameraDirector.GoTo(transmittancePose, 40f);
            Refresh();
        }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Piece || k == RackView.Kind.Garment, null, null));
            if (depth == 1)
            {
                var m = Plaque("材料透光", "对照");
                m.body = "同色的素纱与绸挂在同一木格前：格栅与叶片透过素纱，在绸后被遮住。这张对照不入库，也不解锁绸。";
                m.cancelLabel = "‹ 回人台";
                m.onCancel = () => Back();
                Show(m);
                return;
            }
            if (depth == 2)
            {
                var g = Play.Find.Garment(S, resultGarmentId);
                var m = Plaque(g.name + "收好了", "入库");
                m.resultTier = Names.Tier(g.tier);
                m.traits.AddRange(g.traits);
                m.source = Names.Source(S, C, g);
                m.details = "原料 " + g.scores.material + "\n成纱 " + g.scores.yarn + "\n织造 " + g.scores.weave + "\n染色 " + (g.scores.dye.HasValue ? g.scores.dye.Value.ToString("0") : "未染（按默认分）")
                            + "\n后整理 未做（按默认分）\n裁剪 " + g.scores.cut?.ToString("0") + "\n缝制 " + g.scores.sew?.ToString("0");
                m.primaryLabel = "走去戏台";
                m.onPrimary = () => Workshop.Approach(Workshop.stations.Find(x => x.stationId == "station_stage"));
                Show(m);
                return;
            }
            string patternId = CurrentPattern();
            var sewn = SewnOfPattern(patternId);
            var pattern = C.patterns.Find(p => p.id == patternId);
            bool complete = true;
            foreach (var slot in pattern.parts)
                if (!pattern.optionalParts.Contains(slot) && !sewn.Exists(p => p.slot == slot)) complete = false;
            var mm = Plaque(sewn.Count == 0 ? (S.garments.Count > 0 ? "人台上是已收好的成衣" : "先去针线把衣片缝好") : complete ? "检查后收成" + pattern.name : "还缺部件");
            mm.body = "A / D 或拖动旋转；只有基础风与中性白光。";
            mm.secondary.Add(new KeyValuePair<string, System.Action>("近看衣领", () => Workshop.cameraDirector.GoTo(collarPose, 30f)));
            mm.secondary.Add(new KeyValuePair<string, System.Action>("近看下摆", () => Workshop.cameraDirector.GoTo(hemPose, 34f)));
            mm.secondary.Add(new KeyValuePair<string, System.Action>(outerHidden ? "显示外层" : "藏外层", () => { outerHidden = !outerHidden; visual.SetOuterVisible(!outerHidden); Refresh(); }));
            mm.secondary.Add(new KeyValuePair<string, System.Action>("材料透光", ShowTransmittance));
            if (sewn.Count > 0)
            {
                mm.primaryLabel = "收成" + pattern.name;
                mm.primaryEnabled = complete;
                mm.onPrimary = () =>
                {
                    var input = new Craft.AssembleInput { previewSeen = true, patternId = patternId };
                    foreach (var p in sewn) input.pieceIds.Add(p.id);
                    var r = G.Run((s, c) => Craft.Assemble(s, c, input));
                    if (r.ok) { resultGarmentId = r.createdId; depth = 2; shownGarmentId = null; ShowCurrent(); }
                    else H.Toast(r.error);
                    Refresh();
                };
            }
            else
            {
                var g = S.garments.Find(x => !S.exhibit.slots.Exists(e => e.itemId == x.id));
                if (g != null)
                {
                    mm.secondary.Add(new KeyValuePair<string, System.Action>("拆衣（布回仓库，边损 +1）", () =>
                    {
                        if (Run((s, c) => Craft.Unpick(s, c, g.id), "已拆开，布回到侧架")) { shownGarmentId = null; ShowCurrent(); Refresh(); }
                    }));
                }
            }
            Show(mm);
        }
    }
}
