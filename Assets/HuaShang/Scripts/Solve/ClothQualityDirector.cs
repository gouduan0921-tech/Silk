using UnityEngine;
using HuaShang.Rules.Config;

namespace HuaShang.Solve
{
    /// <summary>
    /// 质量档的统一执行者（docs/03 §3）。
    /// 全局档由设置给出；舞台同时只有一位角色用高或极致；展柜 N 米内的一件升到高，其余保持中。
    /// </summary>
    public class ClothQualityDirector : MonoBehaviour
    {
        public static ClothQualityDirector Instance { get; private set; }

        public ClothQuality globalQuality = ClothQuality.High;
        /// <summary>舞台上当前用高档布料的角色。</summary>
        public string stageFocusOwner;
        public Transform viewer;
        public ClothFixed fixedSettings;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void LateUpdate() { ApplyAll(); }

        public void ApplyAll()
        {
            ClothPart nearestExhibit = null;
            float nearest = float.MaxValue;
            if (viewer != null && fixedSettings != null)
            {
                foreach (var p in ClothPart.All)
                {
                    if (p.context != ClothContext.Exhibit) continue;
                    float dist = Vector3.Distance(viewer.position, p.transform.position);
                    if (dist <= fixedSettings.museumNearMeters && dist < nearest) { nearest = dist; nearestExhibit = p; }
                }
            }
            foreach (var p in ClothPart.All)
            {
                var q = EffectiveQuality(p, nearestExhibit);
                p.SetSimulated(ClothQualityRules.Simulates(q, p.layer, p.layered));
                p.SetSelfCollision(ClothQualityRules.SelfCollision(q));
            }
        }

        public ClothQuality EffectiveQuality(ClothPart p, ClothPart nearestExhibit)
        {
            var cap = ClothQuality.Ultra;
            if (p.context == ClothContext.Stage && !string.IsNullOrEmpty(stageFocusOwner) && p.ownerId != stageFocusOwner)
                cap = ClothQuality.Medium;
            if (p.context == ClothContext.Exhibit)
                cap = (nearestExhibit != null && nearestExhibit.ownerId == p.ownerId) ? ClothQuality.High : ClothQuality.Medium;
            return (ClothQuality)Mathf.Min((int)globalQuality, (int)cap);
        }
    }
}
