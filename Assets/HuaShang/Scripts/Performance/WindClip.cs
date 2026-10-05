using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    // 一条时间轴四条轨道：动画、MagicaWindZone、灯、摄像机（docs/08 §3）。
    // 打包时 ScriptableObject / MonoBehaviour 的类名必须与文件名一致，所以每个可序列化类单独一个文件。

    // ---------------- 风 ----------------

    [Serializable]
    public class WindBehaviour : PlayableBehaviour
    {
        /// <summary>风速（米/秒），来自 docs/04 §9。</summary>
        public float speed;
        /// <summary>舞台局部坐标下的风向。</summary>
        public Vector3 direction = Vector3.right;
    }

    [Serializable]
    public class WindClip : PlayableAsset, ITimelineClipAsset
    {
        public WindBehaviour template = new WindBehaviour();
        public ClipCaps clipCaps => ClipCaps.Blending;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<WindBehaviour>.Create(graph, template);
    }

    public class WindMixer : PlayableBehaviour
    {
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var zone = playerData as MagicaWindZone;
            if (zone == null) return;
            float speed = 0, total = 0;
            Vector3 dir = Vector3.zero;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= 0) continue;
                var b = ((ScriptPlayable<WindBehaviour>)playable.GetInput(i)).GetBehaviour();
                speed += b.speed * w;
                dir += b.direction * w;
                total += w;
            }
            zone.enabled = total > 0.001f && speed > 0.0001f;
            if (!zone.enabled) return;
            zone.main = speed;
            zone.SetWindDirection(dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.right, true);
        }
    }
}
