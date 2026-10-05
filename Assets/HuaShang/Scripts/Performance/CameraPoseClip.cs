using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    [Serializable]
    public class CameraPoseBehaviour : PlayableBehaviour
    {
        /// <summary>舞台局部坐标：片段开始与结束的机位、注视点。0 档只有一条路径，不快切（docs/15 §4）。</summary>
        public Vector3 fromPosition, toPosition;
        public Vector3 lookAt = new Vector3(0, 1.1f, 0);
        public float fov = 35f;
    }

    [Serializable]
    public class CameraPoseClip : PlayableAsset, ITimelineClipAsset
    {
        public CameraPoseBehaviour template = new CameraPoseBehaviour();
        public ClipCaps clipCaps => ClipCaps.Blending;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<CameraPoseBehaviour>.Create(graph, template);
    }

    public class CameraPoseMixer : PlayableBehaviour
    {
        /// <summary>舞台根节点：片段坐标以它为准。</summary>
        public static Transform stageRoot;
        /// <summary>玩家接管镜头时（30 档切换或自由相机）不写摄像机。</summary>
        public static bool overridden;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var cam = playerData as Camera;
            if (cam == null || stageRoot == null || overridden) return;
            Vector3 pos = Vector3.zero, look = Vector3.zero;
            float fov = 0, total = 0;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= 0) continue;
                var input = (ScriptPlayable<CameraPoseBehaviour>)playable.GetInput(i);
                var b = input.GetBehaviour();
                float k = input.GetDuration() > 0 ? (float)(input.GetTime() / input.GetDuration()) : 0;
                pos += Vector3.Lerp(b.fromPosition, b.toPosition, Mathf.SmoothStep(0, 1, k)) * w;
                look += b.lookAt * w;
                fov += b.fov * w;
                total += w;
            }
            if (total <= 0) return;
            pos /= total; look /= total; fov /= total;
            cam.transform.position = stageRoot.TransformPoint(pos);
            cam.transform.rotation = Quaternion.LookRotation(stageRoot.TransformPoint(look) - cam.transform.position, Vector3.up);
            cam.fieldOfView = fov;
        }
    }
}
