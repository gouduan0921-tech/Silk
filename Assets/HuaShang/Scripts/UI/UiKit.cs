using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HuaShang.Game;

namespace HuaShang.UI
{
    /// <summary>键盘焦点外线：暖杏色（风格规范「焦点」）。</summary>
    public class FocusFrame : MonoBehaviour, ISelectHandler, IDeselectHandler
    {
        Outline outline;

        void Awake()
        {
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = UiTheme.Focus;
            outline.effectDistance = new Vector2(3, -3);
            outline.enabled = false;
        }

        public void OnSelect(BaseEventData e) { if (outline) outline.enabled = true; }
        public void OnDeselect(BaseEventData e) { if (outline) outline.enabled = false; }
    }

    /// <summary>代码搭 uGUI 的小工具。所有字号乘设置里的文字倍率，布局尺寸不乘，因而命中区不动。</summary>
    public static class UiKit
    {
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, bool song = false, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = song ? UiTheme.Song : UiTheme.Sans;
            t.fontSize = Mathf.RoundToInt(size * Settings.Current.TextScale);
            t.color = color;
            t.text = text;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.15f;
            t.raycastTarget = false;
            var fit = rt.gameObject.AddComponent<LayoutElement>();
            fit.minHeight = t.fontSize * 1.3f;
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, bool primary, bool enabled = true)
        {
            var img = Panel("Button_" + label, parent, primary ? UiTheme.Indigo : UiTheme.Paper);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = UiTheme.ButtonHeight;
            le.preferredHeight = UiTheme.ButtonHeight;
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.86f, 0.86f, 0.86f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.selectedColor = new Color(0.92f, 0.92f, 0.92f);
            colors.disabledColor = new Color(1, 1, 1, 0.45f);
            btn.colors = colors;
            btn.interactable = enabled;
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<FocusFrame>();
            if (!primary)
            {
                var o = img.gameObject.AddComponent<Outline>();
                o.effectColor = UiTheme.PaperEdge;
                o.effectDistance = new Vector2(1, -1);
            }
            var t = Label(img.transform, label, UiTheme.Body, primary ? UiTheme.GauzeWhite : UiTheme.Ink, false, TextAnchor.MiddleCenter);
            Stretch((RectTransform)t.transform, 6);
            return btn;
        }

        /// <summary>选项小木片：选中为淡灰绿底与深靛蓝边；文字与状态同时变化。</summary>
        public static Button Chip(Transform parent, string label, bool selected, Action onClick)
        {
            var img = Panel("Chip_" + label, parent, selected ? UiTheme.Selected : UiTheme.Paper);
            var le = img.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 36; le.preferredHeight = 36; le.flexibleWidth = 1;
            var btn = img.gameObject.AddComponent<Button>();
            if (onClick != null) btn.onClick.AddListener(() => onClick());
            img.gameObject.AddComponent<FocusFrame>();
            var o = img.gameObject.AddComponent<Outline>();
            o.effectColor = selected ? UiTheme.Indigo : UiTheme.PaperEdge;
            o.effectDistance = selected ? new Vector2(2, -2) : new Vector2(1, -1);
            var t = Label(img.transform, selected ? "✓ " + label : label, UiTheme.Body, UiTheme.Ink, false, TextAnchor.MiddleCenter);
            Stretch((RectTransform)t.transform, 2);
            return btn;
        }

        public static void Stretch(RectTransform rt, float inset = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static VerticalLayoutGroup Vertical(GameObject go, float spacing, int padding)
        {
            var v = go.GetComponent<VerticalLayoutGroup>(); // 重建时组件已在
            if (v == null) v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlHeight = true;
            v.childControlWidth = true;
            v.childForceExpandHeight = false;
            v.childForceExpandWidth = true;
            return v;
        }

        public static HorizontalLayoutGroup Horizontal(GameObject go, float spacing)
        {
            var h = go.GetComponent<HorizontalLayoutGroup>(); // 重建时组件已在
            if (h == null) h = go.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlHeight = true;
            h.childControlWidth = true;
            h.childForceExpandHeight = false;
            h.childForceExpandWidth = true;
            return h;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }
    }
}
