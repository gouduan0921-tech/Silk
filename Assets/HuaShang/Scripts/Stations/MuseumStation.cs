using System.Collections.Generic;
using UnityEngine;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Save;
using HuaShang.Solve;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>
    /// 展柜（docs/09）：放入前是空柜；放入后侧架移走这件，柜内可见。说明牌标题不超过 40 字，开关原料、染层、汉风。
    /// 不足开幕件数时只是单柜预览：不开幕、不计分、不发丝钱（F5）。每天第一次开馆结算一次。
    /// 柜子上的布用中档模拟，走近升到高（docs/03 §3）。
    /// </summary>
    public class MuseumStation : StationBase
    {
        public readonly List<Transform> slotAnchors = new List<Transform>();
        readonly Dictionary<int, GameObject> displays = new Dictionary<int, GameObject>();
        int selectedSlot;
        string pickId;
        RackView.Kind pickKind;
        string draftTitle;
        bool draftMat, draftDye, draftDyn;

        Renderer glassRenderer;

        /// <summary>玻璃罩是程序材质（透明、关反射），换外形时留着。</summary>
        public override IEnumerable<Renderer> LiveRenderers() { if (glassRenderer != null) yield return glassRenderer; }

        public void BuildProps()
        {
            var root = transform;
            Props.Box(root, "Wall", new Vector3(0, 1.8f, 1.2f), new Vector3(4f, 3.6f, 0.1f), Props.Wall);
            Props.Box(root, "CaseBase", new Vector3(0, 0.3f, 0), new Vector3(2.6f, 0.6f, 1.2f), Props.Wood);
            var glass = Props.Box(root, "Glass", new Vector3(0, 1.45f, 0), new Vector3(2.6f, 1.7f, 1.2f), Color.white);
            // 透明、关环境反射：模板里已设好（灰盒天空盒偏蓝，玻璃反射会整块发青）
            var gm = LitMaterials.New(LitMaterials.Kind.Glass, new Color(0.85f, 0.9f, 0.92f, 0.08f));
            gm.SetFloat("_Smoothness", 0.9f);
            var gr = glass.GetComponent<MeshRenderer>();
            glassRenderer = gr;
            gr.sharedMaterial = gm;
            gr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // 玻璃不投影，否则柜内整片发暗
            gr.receiveShadows = false;
            for (int i = 0; i < 2; i++)
            {
                var a = new GameObject("Slot_" + i).transform;
                a.SetParent(root, false);
                a.localPosition = new Vector3(i == 0 ? -0.62f : 0.62f, 0.6f, 0);
                slotAnchors.Add(a);
            }
            var warm = new GameObject("CaseLight").AddComponent<Light>();
            warm.transform.SetParent(root, false);
            warm.transform.localPosition = new Vector3(0, 2.6f, -0.6f);
            warm.transform.LookAt(root.TransformPoint(new Vector3(0, 1f, 0)));
            warm.type = LightType.Spot; warm.spotAngle = 70; warm.range = 6; warm.intensity = 2.5f;
            warm.useColorTemperature = true; warm.colorTemperature = 3800;
            var col = gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 1.2f, 0); col.size = new Vector3(2.8f, 2.4f, 1.4f);
        }

        public override bool Done => S.exhibit.slots.Count > 0;

        public override void OnApproach() { Sync(); }

        public override void OnEnter()
        {
            base.OnEnter();
            Sync();
            selectedSlot = S.exhibit.slots.Exists(x => x.index == 0) && !S.exhibit.slots.Exists(x => x.index == 1) ? 1 : 0;
            // 每天第一次开馆结算一次；不足开幕件数不结算（docs/09 §4）
            int coins = Exhibits.SettleToday(S, C, out double score);
            if (coins > 0 || (Exhibits.CanOpen(S, C) && S.exhibit.lastPaidDay == S.dayIndex && score > 0))
            {
                G.NotifyChanged();
                H.Toast("开幕结算：展览分 " + score.ToString("0") + "，丝钱 +" + coins);
            }
        }

        /// <summary>柜内显示与存档一致。</summary>
        public void Sync()
        {
            for (int i = 0; i < slotAnchors.Count; i++)
            {
                var slot = S.exhibit.slots.Find(x => x.index == i);
                if (displays.TryGetValue(i, out var old) && old != null) Destroy(old);
                displays.Remove(i);
                if (slot == null) continue;
                var holder = new GameObject("Display_" + i);
                holder.transform.SetParent(slotAnchors[i], false);
                if (slot.itemKind == "garment")
                {
                    var g = Play.Find.Garment(S, slot.itemId);
                    if (g == null) continue;
                    var body = GreyboxBody.Create("Form", GreyboxBody.FacingPivot(holder.transform), Props.Mat(new Color(0.82f, 0.8f, 0.76f)));
                    body.transform.localScale = Vector3.one * 0.62f;
                    body.ApplyCharacterArt("form");
                    var vis = body.gameObject.AddComponent<GarmentVisual>();
                    vis.body = body;
                    vis.context = ClothContext.Exhibit;
                    vis.ownerId = g.id;
                    vis.BuildGarment(S, C, g);
                }
                else
                {
                    var b = Play.Find.Bolt(S, slot.itemId);
                    if (b == null) continue;
                    var folded = Props.Box(holder.transform, "FoldedBolt", new Vector3(0, 0.08f, 0), new Vector3(0.5f, 0.14f, 0.35f), Names.BoltColor(C, b));
                    folded.GetComponent<MeshRenderer>().sharedMaterial = new Material(Props.Mat(Color.white)) { color = Names.BoltColor(C, b) };
                }
                displays[i] = holder;
            }
        }

        public override bool Back()
        {
            if (depth == 1) { depth = 0; Refresh(); return true; } // 编辑说明牌后取消：原牌不变
            return base.Back();
        }

        public override void Refresh()
        {
            H.SetRack(RackView.Build(S, C, k => k == RackView.Kind.Garment || k == RackView.Kind.Bolt, pickId,
                (k, id) => { pickKind = k; pickId = id; Refresh(); }));
            var slot = S.exhibit.slots.Find(x => x.index == selectedSlot);
            if (depth == 1 && slot != null)
            {
                var m = Plaque("说明牌", "第 " + (selectedSlot + 1) + " 展位");
                m.textEntry = new TextEntry { label = "标题（不超过 " + Exhibits.TitleMaxChars + " 字）", value = draftTitle, maxChars = Exhibits.TitleMaxChars, onChange = v => draftTitle = v };
                m.toggles.Add(new ToggleItem { label = "原料", on = draftMat, onChange = v => draftMat = v });
                m.toggles.Add(new ToggleItem { label = "染料层", on = draftDye, onChange = v => draftDye = v });
                m.toggles.Add(new ToggleItem { label = "汉风", on = draftDyn, onChange = v => draftDyn = v });
                m.primaryLabel = "保存说明牌";
                m.onPrimary = () => { if (Run((s, c) => Exhibits.EditLabel(s, selectedSlot, draftTitle, draftMat, draftDye, draftDyn), "说明牌已更新")) { depth = 0; Refresh(); } };
                m.cancelLabel = "‹ 取消编辑（原牌不变）";
                m.onCancel = () => Back();
                Show(m);
                return;
            }
            int count = S.exhibit.slots.Count;
            var mm = Plaque(count == 0 ? "空柜：从侧架放入一件" : Exhibits.CanOpen(S, C) ? "展柜已开幕" : "单柜预览");
            mm.options.Add(new OptionGroup { label = "展位", choices = new List<string> { "第一展位", "第二展位" }, selected = selectedSlot, onSelect = i => selectedSlot = i });
            mm.options.Add(new OptionGroup
            {
                label = "主题",
                choices = new List<string> { "一色", "从蚕到丝" },
                selected = S.exhibit.theme == Exhibits.ThemeCanToSi ? 1 : 0,
                onSelect = i => { Exhibits.SetTheme(S, i == 1 ? Exhibits.ThemeCanToSi : Exhibits.ThemeYiSe); G.NotifyChanged(); },
            });
            if (slot != null)
            {
                var l = slot.label ?? new ExhibitLabel();
                mm.source = "说明牌：" + (string.IsNullOrEmpty(l.title) ? "（未题）" : l.title) + (l.showMaterial ? " · 原料" : "") + (l.showDyeLayers ? " · 染层" : "") + (l.showDynasty ? " · 汉风" : "");
                mm.secondary.Add(new KeyValuePair<string, System.Action>("编辑说明牌", () =>
                {
                    draftTitle = l.title; draftMat = l.showMaterial; draftDye = l.showDyeLayers; draftDyn = l.showDynasty;
                    depth = 1; Refresh();
                }));
                mm.secondary.Add(new KeyValuePair<string, System.Action>("取出", () => { if (Run((s, c) => Exhibits.Remove(s, selectedSlot))) { Sync(); Refresh(); } }));
            }
            mm.body = count < C.balance.exhibit.minItems
                ? "单柜只预览：不开幕、不计展览分、不发丝钱。凑满 " + C.balance.exhibit.minItems + " 件才开幕结算。"
                : "今天" + (S.exhibit.lastPaidDay == S.dayIndex ? "已结算。" : "开馆时结算一次。");
            mm.primaryLabel = pickId != null ? "放入第 " + (selectedSlot + 1) + " 展位" : null;
            mm.onPrimary = () =>
            {
                string kind = pickKind == RackView.Kind.Garment ? "garment" : "bolt";
                if (Run((s, c) => Exhibits.Place(s, c, selectedSlot, kind, pickId), "已放入展柜"))
                {
                    pickId = null; Sync();
                    if (Exhibits.CanOpen(S, C))
                    {
                        int coins = Exhibits.SettleToday(S, C, out double score);
                        if (coins > 0) H.Toast("开幕结算：展览分 " + score.ToString("0") + "，丝钱 +" + coins);
                        G.NotifyChanged();
                    }
                    Refresh();
                }
            };
            Show(mm);
        }
    }
}
