using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using HuaShang.Greybox;
using HuaShang.Performance;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Save;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 古典木色戏台与上场门（docs/08、docs/15 §5）。上场门只显示已解锁、且有时间轴的角色与档位；
    /// 关闭的角色不出现（发布门第 7 条）。
    /// </summary>
    public class StageStation : StationBase
    {
        public PerformanceDirector performance;
        string characterId = "xiShi";
        int tier;
        string garmentId;
        string presentMode = ClothDescribe.ModeStandard;
        bool lockNeutral;
        string lastPerformanceId;

        static readonly string[] Modes = { ClothDescribe.ModeHistory, ClothDescribe.ModeStandard, ClothDescribe.ModeEnhanced };
        static readonly string[] ModeNames = { "历史还原", "标准", "视觉增强" };

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "Platform", new Vector3(0, 0.25f, 0), new Vector3(5f, 0.5f, 3.4f), Props.Wood);
            Props.Box(root, "Backdrop", new Vector3(0, 2.2f, 1.6f), new Vector3(5.2f, 3.6f, 0.1f), new Color(0.22f, 0.18f, 0.15f));
            foreach (var x in new[] { -2.4f, 2.4f })
            {
                Props.Box(root, "Pillar", new Vector3(x, 2.1f, 1.4f), new Vector3(0.22f, 3.6f, 0.22f), new Color(0.3f, 0.2f, 0.15f));
            }
            Props.Box(root, "Lintel", new Vector3(0, 3.95f, 1.4f), new Vector3(5.2f, 0.25f, 0.3f), new Color(0.3f, 0.2f, 0.15f));

            var stageRoot = new GameObject("StageRoot").transform;
            stageRoot.SetParent(root, false);
            stageRoot.localPosition = new Vector3(0, 0.5f, 0);
            var body = GreyboxBody.Create("SK_xiShi_Stage", stageRoot, Props.Mat(new Color(0.8f, 0.76f, 0.72f)));
            body.gameObject.AddComponent<Animator>();
            var vis = body.gameObject.AddComponent<GarmentVisual>();
            vis.body = body;
            vis.context = ClothContext.Stage;

            var wind = new GameObject("StageWind").AddComponent<MagicaWindZone>();
            wind.transform.SetParent(stageRoot, false);
            wind.mode = MagicaWindZone.Mode.BoxDirection;
            wind.size = new Vector3(5f, 4f, 4f);
            wind.enabled = false;

            var lightsGo = new GameObject("StageLights");
            lightsGo.transform.SetParent(stageRoot, false);
            var lights = lightsGo.AddComponent<StageLights>();
            lights.key = MakeLight(lightsGo.transform, "KeyLight", new Vector3(-1.8f, 3.2f, -2.4f), new Vector3(0, 1.1f, 0), 1.2f);
            lights.rim = MakeLight(lightsGo.transform, "RimLight", new Vector3(1.6f, 2.6f, 1.6f), new Vector3(0, 1.3f, 0), 0f);
            lights.rim.enabled = false;

            var dirGo = new GameObject("PerformanceDirector");
            dirGo.transform.SetParent(root, false);
            var pd = dirGo.AddComponent<PlayableDirector>();
            pd.playOnAwake = false;
            performance = dirGo.AddComponent<PerformanceDirector>();
            performance.director = pd;
            performance.stageRoot = stageRoot;
            performance.performer = body;
            performance.visual = vis;
            performance.stageWind = wind;
            performance.lights = lights;

            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.5f, 0.5f); col.size = new Vector3(5.4f, 3.4f, 3.6f);
        }

        static Light MakeLight(Transform parent, string name, Vector3 pos, Vector3 look, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.LookAt(parent.TransformPoint(look));
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 55f;
            l.range = 9f;
            l.intensity = intensity;
            l.shadows = LightShadows.Soft;
            return l;
        }

        public override bool Done => S.performances.Count > 0;

        public override void OnEnter()
        {
            base.OnEnter();
            var g = S.garments.Find(x => !S.exhibit.slots.Exists(e => e.itemId == x.id));
            garmentId = g?.id;
            ShowOnPerformer();
        }

        public override void OnApproach() { ShowOnPerformer(); }

        void ShowOnPerformer()
        {
            var g = garmentId != null ? Play.Find.Garment(S, garmentId) : S.garments.Find(x => !S.exhibit.slots.Exists(e => e.itemId == x.id));
            if (g != null) { performance.visual.ownerId = characterId; performance.visual.BuildGarment(S, C, g, presentMode); }
            else performance.visual.Clear();
        }

        public override bool Back()
        {
            if (performance.Playing) return true; // 演出中 Esc 是暂停
            if (depth == 2) { depth = 0; Refresh(); return true; }
            return base.Back();
        }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Garment, garmentId,
                (k, id) => { garmentId = id; ShowOnPerformer(); Refresh(); }));
            if (depth == 2)
            {
                var rec = S.performances.Find(p => p.id == lastPerformanceId);
                var m = Plaque("演出完成", "定格");
                if (rec != null)
                {
                    m.source = Names.Character(C, rec.characterId) + " · " + rec.tier + " 档 · " + ModeNames[System.Array.IndexOf(Modes, rec.presentMode ?? ClothDescribe.ModeStandard)];
                    m.details = "契合 " + rec.fit?.ToString("0") + "\n热度 " + rec.heat?.ToString("0") + "\n好感 " + Play.Find.Character(S, rec.characterId).affection;
                }
                m.primaryLabel = "走去展柜";
                m.onPrimary = () => Workshop.Approach(Workshop.stations.Find(x => x.stationId == "station_museum"));
                Show(m);
                return;
            }
            var door = Stage.StageDoor(S, C, performance.HasTimeline);
            if (!door.Contains(characterId)) characterId = door.Count > 0 ? door[0] : null;
            var mm = Plaque("上场门");
            if (characterId != null)
            {
                var cg = new OptionGroup { label = "上场", choices = door.ConvertAll(id => Names.Character(C, id)), selected = door.IndexOf(characterId) };
                cg.onSelect = i => { characterId = door[i]; tier = 0; };
                mm.options.Add(cg);
                var tiers = Stage.AvailableTiers(S, C, characterId).FindAll(t => performance.HasTimeline(characterId, t));
                if (!tiers.Contains(tier)) tier = tiers.Count > 0 ? tiers[0] : 0;
                mm.options.Add(new OptionGroup { label = "档位", choices = tiers.ConvertAll(t => t + " 档"), selected = tiers.IndexOf(tier), onSelect = i => tier = tiers[i] });
                mm.options.Add(new OptionGroup { label = "呈现", choices = new List<string>(ModeNames), selected = System.Array.IndexOf(Modes, presentMode), onSelect = i => { presentMode = Modes[i]; ShowOnPerformer(); } });
                mm.options.Add(new OptionGroup { label = "主光", choices = new List<string> { "按衣色推荐", "锁定素光" }, selected = lockNeutral ? 1 : 0, onSelect = i => lockNeutral = i == 1 });
            }
            var g = garmentId != null ? Play.Find.Garment(S, garmentId) : null;
            mm.body = g == null ? "先在人台收好一件成衣。" : "演出中只留暂停；字幕默认开。" + (tier >= 30 ? "按 C 切换镜头，按住右键转自由相机。" : "");
            mm.primaryLabel = "开演";
            mm.primaryEnabled = g != null && characterId != null;
            mm.onPrimary = () =>
            {
                float locked = 0;
                if (lockNeutral) { var t = C.balance.windLight.colorTemps.Find(x => x.label == "素"); locked = t != null ? t.kelvin : 0; }
                Workshop.performing = true;
                performance.onFinished = id => { Workshop.performing = false; lastPerformanceId = id; depth = id != null ? 2 : 0; Workshop.RefreshChrome(); Refresh(); };
                performance.Play(characterId, tier, garmentId, presentMode, locked);
            };
            Show(mm);
        }
    }
}
