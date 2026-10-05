using UnityEngine;

namespace HuaShang.Probe
{
    /// <summary>
    /// 灰盒 0 档动作的替身：原地站姿里缓慢转身、轻微起伏，用来做 docs/03 §5 第 4 条的 30 秒压力测试。
    /// 正式动作来自西施的动画（docs/08）；这里不定义演出时长。
    /// </summary>
    public class ProbeMotion : MonoBehaviour
    {
        [Tooltip("左右转身的幅度（度）")] public float yawAmplitude = 35f;
        [Tooltip("一次往返的秒数")] public float period = 6f;
        [Tooltip("起伏幅度（米）")] public float bob = 0.02f;
        public float elapsed;

        Vector3 basePos;
        Quaternion baseRot;

        void Start()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            float w = Mathf.PI * 2f / period;
            transform.localRotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(elapsed * w) * yawAmplitude, 0f);
            transform.localPosition = basePos + Vector3.up * (Mathf.Sin(elapsed * w * 2f) * bob);
        }
    }
}
