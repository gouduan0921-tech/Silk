using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    [TrackColor(0.6f, 0.6f, 0.6f)]
    [TrackClipType(typeof(CameraPoseClip))]
    [TrackBindingType(typeof(Camera))]
    public class CameraPoseTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<CameraPoseMixer>.Create(graph, inputCount);
    }
}
