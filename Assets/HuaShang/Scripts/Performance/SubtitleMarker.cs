using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    /// <summary>字幕标记：0 档没有角色语音，字幕写动作（docs/18 §1）。挂在摄像机轨道上。</summary>
    public class SubtitleMarker : Marker, INotification
    {
        [TextArea] public string text;
        public PropertyName id => new PropertyName("subtitle");
    }
}
