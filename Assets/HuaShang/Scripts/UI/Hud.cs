using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using HuaShang.Game;

namespace HuaShang.UI
{
    /// <summary>
    /// 正式界面的四边结构（构架文档 §3.1）：页首季节与资源，左侧一张操作木牌，右侧实物侧架，底边工位走廊，
    /// 中心约六成留给布和衣服。放大文字只改字号，不改布局与 3D 命中区。
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public static Hud I { get; private set; }

        Canvas canvas;
        CanvasScaler scaler;
        RectTransform root, top, left, plaque, rack, rackList, corridor, toast, subtitle, pause, settings;
        Text topCenter, headerKicker, headerTitle, headerSub, toastText, subtitleText;
        PlaqueModel plaqueModel;
        List<RackItem> rackItems = new List<RackItem>();
        List<CorridorStop> stops = new List<CorridorStop>();
        string topInfo = "";
        string hKicker = "", hTitle = "", hSub = "";
        float toastUntil;
        bool performance;
        Action onPause;
        bool paused;
        public Action onTransmittance;
        public bool settingsOpen;

        void Awake()
        {
            I = this;
            EnsureEventSystem();
            var go = new GameObject("HUD", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            go.AddComponent<GraphicRaycaster>();
            root = (RectTransform)go.transform;
            Settings.Changed += Rebuild;
            Rebuild();
        }

        void OnDestroy()
        {
            Settings.Changed -= Rebuild;
            if (I == this) I = null;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        int lastW, lastH;

        void Update()
        {
            if (Screen.width != lastW || Screen.height != lastH) Rebuild();
            if (toast != null) toast.gameObject.SetActive(Time.unscaledTime < toastUntil && !performance);
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tabKey.wasPressedThisFrame) CycleFocus(kb.shiftKey.isPressed ? -1 : 1);
            if ((kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                && EventSystem.current != null && EventSystem.current.currentSelectedGameObject == null
                && plaqueModel != null && plaqueModel.onPrimary != null && plaqueModel.primaryEnabled && !performance)
                plaqueModel.onPrimary();
        }

        /// <summary>Tab 依木牌、侧架、走廊、页首的顺序移动可见焦点（docs/12 §1、docs/22 §2）。</summary>
        void CycleFocus(int dir)
        {
            var list = new List<Selectable>();
            foreach (var area in new[] { plaque, rack, corridor, top })
                if (area != null && area.gameObject.activeInHierarchy)
                    foreach (var s in area.GetComponentsInChildren<Selectable>())
                        if (s.IsInteractable() && s.gameObject.activeInHierarchy) list.Add(s);
            if (list.Count == 0) return;
            var cur = EventSystem.current.currentSelectedGameObject;
            int idx = cur == null ? -1 : list.FindIndex(x => x.gameObject == cur);
            int next = idx < 0 ? (dir > 0 ? 0 : list.Count - 1) : (idx + dir + list.Count) % list.Count;
            list[next].Select();
        }

        // ---------------- 对外 ----------------

        public void SetTopInfo(string info) { topInfo = info; if (topCenter) topCenter.text = info; }

        public void SetHeader(string kicker, string title, string sub)
        {
            hKicker = kicker; hTitle = title; hSub = sub;
            if (headerKicker) { headerKicker.text = kicker; headerTitle.text = title; headerSub.text = sub; }
        }

        public void ShowPlaque(PlaqueModel m)
        {
            plaqueModel = m;
            if (EventSystem.current) EventSystem.current.SetSelectedGameObject(null);
            BuildPlaque();
        }

        public PlaqueModel CurrentPlaque => plaqueModel;

        public void SetRack(List<RackItem> items) { rackItems = items ?? new List<RackItem>(); BuildRack(); }
        public void SetCorridor(List<CorridorStop> s) { stops = s ?? new List<CorridorStop>(); BuildCorridor(); }

        public void Toast(string msg, float seconds = 3f)
        {
            if (string.IsNullOrEmpty(msg)) return;
            toastText.text = msg;
            toastUntil = Time.unscaledTime + seconds;
        }

        /// <summary>字幕：默认开（docs/01 §8）；0 档无角色语音，字幕写动作。</summary>
        public void Subtitle(string text)
        {
            bool show = !string.IsNullOrEmpty(text) && Settings.Current.subtitles;
            subtitle.gameObject.SetActive(show);
            subtitleText.text = text ?? "";
        }

        /// <summary>演出中隐藏工位木牌、侧架、走廊与页首，只留暂停（docs/12 §4）。</summary>
        public void SetPerformanceMode(bool on, Action pauseToggle)
        {
            performance = on;
            onPause = pauseToggle;
            paused = false;
            Rebuild();
        }

        public void SetPausedLabel(bool isPaused) { paused = isPaused; BuildPause(); }

        // ---------------- 搭建 ----------------

        void Rebuild()
        {
            lastW = Screen.width; lastH = Screen.height;
            // 1920×1080 与 1280×720 两档按原尺寸，其他分辨率按高度等比
            scaler.scaleFactor = Screen.height / (UiTheme.Small ? 720f : 1080f) * (UiTheme.Small ? Mathf.Max(1f, 1f) : 1f);
            UiKit.Clear(root);

            top = UiKit.Panel("TopBar", root, UiTheme.Paper).rectTransform;
            Anchor(top, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -UiTheme.TopBar), Vector2.zero);
            BuildTop();

            var leftGo = UiKit.Rect("LeftColumn", root);
            left = leftGo;
            Anchor(left, new Vector2(0, 0), new Vector2(0, 1), new Vector2(UiTheme.PlaqueMargin, UiTheme.Corridor + 24), new Vector2(UiTheme.PlaqueMargin + UiTheme.PlaqueWidth, -UiTheme.TopBar - 24));
            UiKit.Vertical(left.gameObject, 10, 0).childAlignment = TextAnchor.UpperLeft;
            headerKicker = UiKit.Label(left, hKicker, UiTheme.Note, UiTheme.GauzeWhite);
            headerTitle = UiKit.Label(left, hTitle, UiTheme.StationName, UiTheme.GauzeWhite, true);
            headerSub = UiKit.Label(left, hSub, UiTheme.Body, UiTheme.GauzeWhite);
            foreach (var t in new[] { headerKicker, headerTitle, headerSub })
            {
                var sh = t.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0, 0, 0, 0.6f);
            }
            var plaqueImg = UiKit.Panel("Plaque", left, new Color(0.96f, 0.94f, 0.89f, 0.97f));
            plaque = plaqueImg.rectTransform;
            UiKit.Vertical(plaque.gameObject, 10, 18);
            var fit = plaque.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var strip = UiKit.Panel("WoodStrip", plaque, UiTheme.Walnut).rectTransform;
            strip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            strip.anchorMin = new Vector2(0, 0); strip.anchorMax = new Vector2(0, 1);
            strip.offsetMin = Vector2.zero; strip.offsetMax = new Vector2(5, 0);
            BuildPlaque();

            rack = UiKit.Panel("Rack", root, UiTheme.Hex(0x594637, 0.88f)).rectTransform;
            Anchor(rack, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-UiTheme.RackMargin - UiTheme.RackWidth, UiTheme.Corridor + 24), new Vector2(-UiTheme.RackMargin, -UiTheme.TopBar - 40));
            UiKit.Vertical(rack.gameObject, 8, 12);
            UiKit.Label(rack, "侧架 · 实物", UiTheme.Body, UiTheme.GauzeWhite, true);
            rackList = UiKit.Rect("Items", rack);
            UiKit.Vertical(rackList.gameObject, 8, 0);
            rackList.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;
            UiKit.Label(rack, "物品随操作增减 · 点击实物取用", UiTheme.Note, UiTheme.PaperEdge);
            BuildRack();

            corridor = UiKit.Panel("Corridor", root, UiTheme.Paper).rectTransform;
            Anchor(corridor, new Vector2(0, 0), new Vector2(1, 0), Vector2.zero, new Vector2(0, UiTheme.Corridor));
            BuildCorridor();

            toast = UiKit.Panel("Toast", root, UiTheme.Indigo).rectTransform;
            Anchor(toast, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-240, UiTheme.Corridor + 30), new Vector2(240, UiTheme.Corridor + 76));
            toastText = UiKit.Label(toast, "", UiTheme.Body, UiTheme.GauzeWhite, false, TextAnchor.MiddleCenter);
            UiKit.Stretch((RectTransform)toastText.transform, 6);
            toast.gameObject.SetActive(false);

            subtitle = UiKit.Panel("Subtitle", root, new Color(0.1f, 0.09f, 0.08f, 0.55f)).rectTransform;
            Anchor(subtitle, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-380, 36), new Vector2(380, 84));
            subtitleText = UiKit.Label(subtitle, "", UiTheme.Body + 2, UiTheme.GauzeWhite, false, TextAnchor.MiddleCenter);
            UiKit.Stretch((RectTransform)subtitleText.transform, 6);
            subtitle.gameObject.SetActive(false);

            pause = UiKit.Rect("PauseArea", root);
            Anchor(pause, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -70), new Vector2(-24, -24));
            BuildPause();

            top.gameObject.SetActive(!performance);
            left.gameObject.SetActive(!performance);
            rack.gameObject.SetActive(!performance);
            corridor.gameObject.SetActive(!performance);
            pause.gameObject.SetActive(performance);
            if (settingsOpen) BuildSettings();
        }

        static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offMin, Vector2 offMax)
        {
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = offMin; rt.offsetMax = offMax;
        }

        void BuildTop()
        {
            var brand = UiKit.Label(top, "华裳", UiTheme.Brand, UiTheme.Ink, true, TextAnchor.MiddleLeft);
            Anchor((RectTransform)brand.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(28, 0), new Vector2(140, 0));
            var sub = UiKit.Label(top, "一幅素纱 · 从蚕到神", UiTheme.Note, UiTheme.OldGrey, false, TextAnchor.MiddleLeft);
            Anchor((RectTransform)sub.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(110, 0), new Vector2(320, 0));
            topCenter = UiKit.Label(top, topInfo, UiTheme.Resource, UiTheme.Ink, false, TextAnchor.MiddleCenter);
            Anchor((RectTransform)topCenter.transform, new Vector2(0.25f, 0), new Vector2(0.75f, 1), Vector2.zero, Vector2.zero);
            var right = UiKit.Rect("TopButtons", top);
            Anchor(right, new Vector2(1, 0), new Vector2(1, 1), new Vector2(-280, 18), new Vector2(-24, -18));
            UiKit.Horizontal(right.gameObject, 10);
            UiKit.Button(right, "材料透光", () => onTransmittance?.Invoke(), false);
            UiKit.Button(right, "设置", () => { settingsOpen = !settingsOpen; Rebuild(); }, false);
        }

        void BuildPlaque()
        {
            if (plaque == null) return;
            for (int i = plaque.childCount - 1; i >= 1; i--) { var c = plaque.GetChild(i).gameObject; c.SetActive(false); Destroy(c); }
            var m = plaqueModel;
            plaque.gameObject.SetActive(m != null && !performance);
            if (m == null) return;

            if (!string.IsNullOrEmpty(m.kicker)) UiKit.Label(plaque, m.kicker, UiTheme.Note, UiTheme.OldGrey);
            if (!string.IsNullOrEmpty(m.title)) UiKit.Label(plaque, m.title, UiTheme.PlaqueTitle, UiTheme.Ink, true);
            if (!string.IsNullOrEmpty(m.resultTier))
            {
                var row = UiKit.Rect("Result", plaque);
                UiKit.Horizontal(row.gameObject, 8).childForceExpandWidth = false;
                UiKit.Label(row, m.resultTier, UiTheme.PlaqueTitle, UiTheme.Indigo, true);
                foreach (var t in m.traits) UiKit.Label(row, "［" + t + "］", UiTheme.Body, UiTheme.Ink);
            }
            if (!string.IsNullOrEmpty(m.source)) UiKit.Label(plaque, m.source, UiTheme.Body, UiTheme.Ink);
            if (!string.IsNullOrEmpty(m.body)) UiKit.Label(plaque, m.body, UiTheme.Body, UiTheme.OldGrey);
            foreach (var w in m.warnings)
            {
                var box = UiKit.Panel("Warning", plaque, UiTheme.Hex(0xF3E6DD));
                var o = box.gameObject.AddComponent<Outline>();
                o.effectColor = UiTheme.Terracotta;
                o.effectDistance = new Vector2(2, -2);
                UiKit.Vertical(box.gameObject, 0, 8);
                UiKit.Label(box.transform, "⚠ " + w, UiTheme.Body, UiTheme.Terracotta);
            }
            if (m.beat != null)
            {
                var bar = UiKit.Panel("BeatBar", plaque, UiTheme.Paper).rectTransform;
                bar.gameObject.AddComponent<LayoutElement>().minHeight = 52;
                bar.gameObject.AddComponent<BeatBar>().Init(m.beat);
            }
            foreach (var g in m.options)
            {
                if (!string.IsNullOrEmpty(g.label)) UiKit.Label(plaque, g.label, UiTheme.Note, UiTheme.OldGrey);
                var row = UiKit.Rect("Options", plaque);
                UiKit.Horizontal(row.gameObject, 6);
                for (int i = 0; i < g.choices.Count; i++)
                {
                    int idx = i;
                    var grp = g;
                    UiKit.Chip(row, g.choices[i], g.selected == i, () => { grp.selected = idx; grp.onSelect?.Invoke(idx); BuildPlaque(); });
                }
            }
            if (m.textEntry != null) BuildTextEntry(m.textEntry);
            foreach (var t in m.toggles)
            {
                var item = t;
                UiKit.Chip(plaque, (item.on ? "开 · " : "关 · ") + item.label, item.on, () => { item.on = !item.on; item.onChange?.Invoke(item.on); BuildPlaque(); });
            }
            if (!string.IsNullOrEmpty(m.primaryLabel)) UiKit.Button(plaque, m.primaryLabel, m.onPrimary, true, m.primaryEnabled);
            foreach (var kv in m.secondary) UiKit.Button(plaque, kv.Key, kv.Value, false);
            if (!string.IsNullOrEmpty(m.details))
            {
                var detailsText = UiKit.Label(plaque, m.details, UiTheme.Note, UiTheme.OldGrey);
                detailsText.gameObject.SetActive(false);
                Button foldBtn = null;
                foldBtn = TextButton("▸ 查看工序记录", () =>
                {
                    bool open = !detailsText.gameObject.activeSelf;
                    detailsText.gameObject.SetActive(open);
                    foldBtn.GetComponentInChildren<Text>().text = (open ? "▾" : "▸") + " 查看工序记录";
                });
                foldBtn.transform.SetSiblingIndex(detailsText.transform.GetSiblingIndex());
            }
            if (m.onCancel != null) TextButton(m.cancelLabel, m.onCancel);
        }

        Button TextButton(string label, Action onClick)
        {
            var rt = UiKit.Rect("TextButton", plaque);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(1, 1, 1, 0.001f);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
            rt.gameObject.AddComponent<FocusFrame>();
            rt.gameObject.AddComponent<LayoutElement>().minHeight = 30;
            var t = UiKit.Label(rt, label, UiTheme.Body, UiTheme.OldGrey, false, TextAnchor.MiddleLeft);
            UiKit.Stretch((RectTransform)t.transform);
            return btn;
        }

        void BuildTextEntry(TextEntry e)
        {
            UiKit.Label(plaque, e.label, UiTheme.Note, UiTheme.OldGrey);
            var box = UiKit.Panel("Input", plaque, Color.white);
            box.gameObject.AddComponent<LayoutElement>().minHeight = 40;
            var input = box.gameObject.AddComponent<InputField>();
            var text = UiKit.Label(box.transform, "", UiTheme.Body, UiTheme.Ink, false, TextAnchor.MiddleLeft);
            UiKit.Stretch((RectTransform)text.transform, 8);
            text.supportRichText = false;
            input.textComponent = text;
            input.characterLimit = e.maxChars;
            input.text = e.value ?? "";
            input.onValueChanged.AddListener(v => e.onChange?.Invoke(v));
            box.gameObject.AddComponent<FocusFrame>();
        }

        void BuildRack()
        {
            if (rackList == null) return;
            UiKit.Clear(rackList);
            foreach (var it in rackItems)
            {
                var item = it;
                var img = UiKit.Panel("Item_" + it.id, rackList, it.selected ? UiTheme.Selected : UiTheme.Hex(0x4A3A2D, 0.9f));
                var le = img.gameObject.AddComponent<LayoutElement>();
                le.minHeight = 64;
                var btn = img.gameObject.AddComponent<Button>();
                btn.interactable = it.interactable;
                if (it.onClick != null) btn.onClick.AddListener(() => item.onClick());
                img.gameObject.AddComponent<FocusFrame>();
                var sw = UiKit.Panel("Swatch", img.transform, it.color).rectTransform;
                Anchor(sw, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(8, -22), new Vector2(52, 22));
                var label = UiKit.Label(img.transform, it.label + "\n" + it.sub, UiTheme.Note, it.selected ? UiTheme.Ink : UiTheme.GauzeWhite, false, TextAnchor.MiddleLeft);
                Anchor((RectTransform)label.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(60, 2), new Vector2(-4, -2));
            }
        }

        void BuildCorridor()
        {
            if (corridor == null) return;
            UiKit.Clear(corridor);
            var row = UiKit.Rect("Stops", corridor);
            Anchor(row, new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(-stops.Count * 68, 8), new Vector2(stops.Count * 68, -8));
            UiKit.Horizontal(row.gameObject, 2).childForceExpandHeight = true;
            foreach (var s in stops)
            {
                var stop = s;
                var img = UiKit.Panel("Stop_" + s.id, row, s.current ? UiTheme.Selected : UiTheme.Paper);
                var btn = img.gameObject.AddComponent<Button>();
                if (s.onClick != null) btn.onClick.AddListener(() => stop.onClick());
                img.gameObject.AddComponent<FocusFrame>();
                var t = UiKit.Label(img.transform, s.number + (s.done ? " / 已完成 ·" : " / 工位") + "\n" + s.label, UiTheme.Note + 1, UiTheme.Ink, false, TextAnchor.MiddleCenter);
                UiKit.Stretch((RectTransform)t.transform, 2);
            }
        }

        void BuildPause()
        {
            if (pause == null) return;
            UiKit.Clear(pause);
            UiKit.Vertical(pause.gameObject, 0, 0);
            UiKit.Button(pause, paused ? "继续" : "暂停", () => onPause?.Invoke(), false);
        }

        void BuildSettings()
        {
            settings = UiKit.Panel("Settings", root, new Color(0.96f, 0.94f, 0.89f, 0.98f)).rectTransform;
            Anchor(settings, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-360, -UiTheme.TopBar - 420), new Vector2(-24, -UiTheme.TopBar - 8));
            UiKit.Vertical(settings.gameObject, 8, 16);
            var s = Settings.Current;
            UiKit.Label(settings, "设置", UiTheme.PlaqueTitle, UiTheme.Ink, true);
            UiKit.Chip(settings, "放大文字", s.largeText, () => { s.largeText = !s.largeText; Settings.Apply(); });
            UiKit.Chip(settings, "减少动效", s.reduceMotion, () => { s.reduceMotion = !s.reduceMotion; Settings.Apply(); });
            UiKit.Chip(settings, "字幕", s.subtitles, () => { s.subtitles = !s.subtitles; Settings.Apply(); });
            UiKit.Chip(settings, "音乐", s.musicVolume > 0, () => { s.musicVolume = s.musicVolume > 0 ? 0 : 0.7f; Settings.Apply(); });
            UiKit.Label(settings, "布料质量", UiTheme.Note, UiTheme.OldGrey);
            var row = UiKit.Rect("Quality", settings);
            UiKit.Horizontal(row.gameObject, 4);
            string[] names = { "低", "中", "高", "极致" };
            for (int i = 0; i < names.Length; i++)
            {
                int q = i;
                UiKit.Chip(row, names[i], s.clothQuality == i, () => { s.clothQuality = q; Settings.Apply(); });
            }
            UiKit.Button(settings, "关闭", () => { settingsOpen = false; Rebuild(); }, false);
        }
    }

    /// <summary>节拍条：游标与目标线；判定由工位按空格完成，这里只显示。</summary>
    public class BeatBar : MonoBehaviour
    {
        Func<BeatView> source;
        RectTransform cursor, target;
        Text caption;

        public void Init(Func<BeatView> src)
        {
            source = src;
            var rt = (RectTransform)transform;
            var track = UiKit.Panel("Track", rt, UiTheme.PaperEdge).rectTransform;
            track.anchorMin = new Vector2(0, 0.6f); track.anchorMax = new Vector2(1, 0.7f);
            track.offsetMin = new Vector2(8, 0); track.offsetMax = new Vector2(-8, 0);
            target = UiKit.Panel("Target", rt, UiTheme.Indigo).rectTransform;
            cursor = UiKit.Panel("Cursor", rt, UiTheme.Walnut).rectTransform;
            caption = UiKit.Label(rt, "", UiTheme.Note, UiTheme.Ink, false, TextAnchor.LowerLeft);
            caption.rectTransform.anchorMin = new Vector2(0, 0); caption.rectTransform.anchorMax = new Vector2(1, 0.5f);
            caption.rectTransform.offsetMin = new Vector2(8, 2); caption.rectTransform.offsetMax = new Vector2(-8, 0);
        }

        void Update()
        {
            if (source == null) return;
            var v = source();
            Place(target, v.target, 4, 22);
            Place(cursor, v.cursor, 8, 14);
            caption.text = v.caption + "  " + (v.total > 0 ? v.done + " / " + v.total : v.done + " 拍") + "（空格）";
        }

        static void Place(RectTransform r, float x, float w, float h)
        {
            r.anchorMin = r.anchorMax = new Vector2(Mathf.Lerp(0.03f, 0.97f, Mathf.Clamp01(x)), 0.65f);
            r.sizeDelta = new Vector2(w, h);
            r.anchoredPosition = Vector2.zero;
        }
    }
}
