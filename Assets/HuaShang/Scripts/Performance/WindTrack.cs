using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    /// <summary>侧风只挂在有转身事件的段落；0 档没有转身就不挂侧风（docs/08 §3）。</summary>
    [TrackColor(0.55f, 0.7f, 0.75f)]
    [TrackClipType(typeof(WindClip))]
    [TrackBindingType(typeof(MagicaWindZone))]
    public class WindTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<WindMixer>.Create(graph, inputCount);
    }
}
