using System;
using System.Collections.Generic;
using UnityEngine;

namespace HuaShang.UI
{
    /// <summary>左侧操作木牌的内容。下一步在最上，取消在最下（设计方案.md）。</summary>
    public class PlaqueModel
    {
        public string kicker = "下一步";
        public string title;
        public string body;
        /// <summary>警告：界面必须同时显示图标与文字，不只靠颜色（docs/12 §6）。</summary>
        public List<string> warnings = new List<string>();
        public List<OptionGroup> options = new List<OptionGroup>();
        public string primaryLabel;
        public Action onPrimary;
        public bool primaryEnabled = true;
        public List<KeyValuePair<string, Action>> secondary = new List<KeyValuePair<string, Action>>();
        public string cancelLabel = "‹ 取消 · 回上一步";
        public Action onCancel;
        /// <summary>完成层：档位、最多 3 个词条、一句来源（docs/12 §2）。</summary>
        public string resultTier;
        public List<string> traits = new List<string>();
        public string source;
        /// <summary>详情层：过程分等，默认收起。</summary>
        public string details;
        /// <summary>节拍条：0–1 的游标位置与目标位置；为空表示不显示。</summary>
        public Func<BeatView> beat;
        /// <summary>说明牌标题输入。</summary>
        public TextEntry textEntry;
        public List<ToggleItem> toggles = new List<ToggleItem>();
    }

    public class OptionGroup
    {
        public string label;
        public List<string> choices = new List<string>();
        public int selected = -1;
        public Action<int> onSelect;
    }

    public class ToggleItem
    {
        public string label;
        public bool on;
        public Action<bool> onChange;
    }

    public class TextEntry
    {
        public string label;
        public string value;
        public int maxChars;
        public Action<string> onChange;
    }

    public struct BeatView
    {
        public float cursor;
        public float target;
        public string caption;
        public int done, total;
    }

    /// <summary>右侧实物侧架的一件。</summary>
    public class RackItem
    {
        public string id;
        public string kind;   // bolt / yarn / dye / piece / garment / cocoon
        public string label;
        public string sub;
        public Color color = Color.white;
        public bool selected;
        public bool interactable = true;
        public Action onClick;
    }

    public class CorridorStop
    {
        public string id;
        public string number;
        public string label;
        public bool current, done;
        public Action onClick;
    }
}
