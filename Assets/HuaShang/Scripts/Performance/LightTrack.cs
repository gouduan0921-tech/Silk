using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    [TrackColor(0.85f, 0.7f, 0.45f)]
    [TrackClipType(typeof(LightClip))]
    [TrackBindingType(typeof(StageLights))]
    public class LightTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<LightMixer>.Create(graph, inputCount);
    }
}
