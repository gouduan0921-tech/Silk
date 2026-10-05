using System;
using MagicaCloth2;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace HuaShang.Performance
{
    // 一条时间轴四条轨道：动画、MagicaWindZone、灯、摄像机（docs/08 §3）。
    // 动画轨道用 Timeline 自带的 AnimationTrack；其余三条在这里。

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

    /// <summary>侧风只挂在有转身事件的段落；0 档没有转身就不挂侧风（docs/08 §3）。</summary>
    [TrackColor(0.55f, 0.7f, 0.75f)]
    [TrackClipType(typeof(WindClip))]
    [TrackBindingType(typeof(MagicaWindZone))]
    public class WindTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<WindMixer>.Create(graph, inputCount);
    }

    // ---------------- 灯 ----------------

    /// <summary>戏台的主光与边缘光。0 档一盏柔光；30 档主光加一条边缘光（docs/08 §2）。</summary>
    public class StageLights : MonoBehaviour
    {
        public Light key;
        public Light rim;
    }

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

    [TrackColor(0.85f, 0.7f, 0.45f)]
    [TrackClipType(typeof(LightClip))]
    [TrackBindingType(typeof(StageLights))]
    public class LightTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<LightMixer>.Create(graph, inputCount);
    }

    // ---------------- 摄像机 ----------------

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

    [TrackColor(0.6f, 0.6f, 0.6f)]
    [TrackClipType(typeof(CameraPoseClip))]
    [TrackBindingType(typeof(Camera))]
    public class CameraPoseTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
            => ScriptPlayable<CameraPoseMixer>.Create(graph, inputCount);
    }

    // ---------------- 字幕 ----------------

    /// <summary>字幕标记：0 档没有角色语音，字幕写动作（docs/18 §1）。挂在摄像机轨道上。</summary>
    public class SubtitleMarker : Marker, INotification
    {
        [TextArea] public string text;
        public PropertyName id => new PropertyName("subtitle");
    }
}
