using UnityEngine;
using HuaShang.Game;
using HuaShang.UI;

namespace HuaShang.Stations
{
    /// <summary>同一场景里推近（docs/12 §1）。减少动效时直接切到目标机位，不做推近过渡。</summary>
    public class CameraDirector : MonoBehaviour
    {
        public Camera cam;
        Vector3 fromPos, toPos;
        Quaternion fromRot, toRot;
        float fromFov, toFov;
        float t = 1f;
        public bool Moving => t < 1f;

        public void GoTo(Transform pose, float fov = 40f)
        {
            if (pose == null) return;
            fromPos = cam.transform.position; fromRot = cam.transform.rotation; fromFov = cam.fieldOfView;
            toPos = pose.position; toRot = pose.rotation; toFov = fov;
            t = Settings.Current.reduceMotion ? 1f : 0f;
            if (t >= 1f) Apply(1f);
        }

        void Update()
        {
            if (t >= 1f) return;
            t = Mathf.Min(1f, t + Time.unscaledDeltaTime / UiTheme.PushInSeconds);
            Apply(Mathf.SmoothStep(0f, 1f, t));
        }

        void Apply(float k)
        {
            cam.transform.position = Vector3.Lerp(fromPos, toPos, k);
            cam.transform.rotation = Quaternion.Slerp(fromRot, toRot, k);
            cam.fieldOfView = Mathf.Lerp(fromFov, toFov, k);
        }
    }
}
