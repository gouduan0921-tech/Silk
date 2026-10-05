using UnityEngine;
using UnityEngine.InputSystem;
using HuaShang.Game;
using HuaShang.Solve;

namespace HuaShang.Probe
{
    /// <summary>
    /// HS_Probe 场景的操作：1–4 切质量档（低/中/高/极致），W 开关侧风，R 重置人体动作。
    /// 只用于 P1 验收，不进正式工位链。
    /// </summary>
    public class ProbeBootstrap : MonoBehaviour
    {
        public ProbeRig rig;

        void Start()
        {
            rig = ProbeRig.Build(GameConfig.Current, transform);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null || rig == null) return;
            if (kb.digit1Key.wasPressedThisFrame) rig.director.globalQuality = ClothQuality.Low;
            if (kb.digit2Key.wasPressedThisFrame) rig.director.globalQuality = ClothQuality.Medium;
            if (kb.digit3Key.wasPressedThisFrame) rig.director.globalQuality = ClothQuality.High;
            if (kb.digit4Key.wasPressedThisFrame) rig.director.globalQuality = ClothQuality.Ultra;
            if (kb.wKey.wasPressedThisFrame) rig.vp2SideWind.enabled = !rig.vp2SideWind.enabled;
        }
    }
}
