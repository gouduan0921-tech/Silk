using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    [Serializable]
    public class LightBehaviour : PlayableBehaviour
    {
        public float keyIntensity = 1f;
        public float kelvin = 5200f;
        public float rimIntensity;
    }

    [Serializable]
    public class LightClip : PlayableAsset, ITimelineClipAsset
    {
        public LightBehaviour template = new LightBehaviour();
        public ClipCaps clipCaps => ClipCaps.Blending;
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) => ScriptPlayable<LightBehaviour>.Create(graph, template);
    }

    public class LightMixer : PlayableBehaviour
    {
        /// <summary>开演前可锁定主光色温（docs/04 §9）；大于 0 时覆盖片段色温。</summary>
        public static float lockedKelvin;
        /// <summary>推荐色温，由成衣主色决定（docs/15 §3）。</summary>
        public static float recommendedKelvin;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var lights = playerData as StageLights;
            if (lights == null) return;
            float key = 0, rim = 0, kelvin = 0, total = 0;
            for (int i = 0; i < playable.GetInputCount(); i++)
            {
                float w = playable.GetInputWeight(i);
                if (w <= 0) continue;
                var b = ((ScriptPlayable<LightBehaviour>)playable.GetInput(i)).GetBehaviour();
                key += b.keyIntensity * w; rim += b.rimIntensity * w; kelvin += b.kelvin * w; total += w;
            }
            if (total <= 0) return;
            if (lights.key != null)
            {
                lights.key.intensity = key;
                lights.key.useColorTemperature = true;
                lights.key.colorTemperature = lockedKelvin > 0 ? lockedKelvin : recommendedKelvin > 0 ? recommendedKelvin : kelvin / total;
            }
            if (lights.rim != null)
            {
                lights.rim.enabled = rim > 0.001f;
                lights.rim.intensity = rim;
            }
        }
    }
}
