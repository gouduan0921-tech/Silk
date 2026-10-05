using System.Collections.Generic;
using MagicaCloth2;
using UnityEngine;

namespace HuaShang.Greybox
{
    /// <summary>
    /// 灰盒人体：躯干、双腿、双臂用胶囊表示，每段带 MagicaCapsuleCollider（docs/03 §2 身体碰撞）。
    /// 西施的 FBX 交付后由正式骨架替换（docs/17 §2）。尺寸为灰盒占位，单位米。
    /// </summary>
    public class GreyboxBody : MonoBehaviour
    {
        public Transform torso, hips, leftLeg, rightLeg;
        public readonly List<ColliderComponent> colliders = new List<ColliderComponent>();
        public readonly List<CapsuleInfo> capsules = new List<CapsuleInfo>();

        public struct CapsuleInfo
        {
            public Transform transform;
            public float radius;
            public float length;
        }

        /// <summary>身高约 1.6 米，脚底在原点。</summary>
        public static GreyboxBody Create(string name, Transform parent, Material skin)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = root.AddComponent<GreyboxBody>();
            body.hips = Segment(body, root.transform, "Hips", new Vector3(0, 0.95f, 0), Vector3.zero, 0.15f, 0.25f, skin);
            body.torso = Segment(body, root.transform, "Torso", new Vector3(0, 1.25f, 0), Vector3.zero, 0.14f, 0.40f, skin);
            Segment(body, root.transform, "Head", new Vector3(0, 1.52f, 0), Vector3.zero, 0.09f, 0.20f, skin);
            body.leftLeg = Segment(body, root.transform, "LegL", new Vector3(-0.08f, 0.45f, 0), Vector3.zero, 0.07f, 0.85f, skin);
            body.rightLeg = Segment(body, root.transform, "LegR", new Vector3(0.08f, 0.45f, 0), Vector3.zero, 0.07f, 0.85f, skin);
            // 灰盒不做手臂：程序筒形上襦无法绕开穿过衣身的手臂，袖与臂留给正式 FBX（docs/17 §3）。
            return body;
        }

        static Transform Segment(GreyboxBody body, Transform parent, string name, Vector3 pos, Vector3 euler, float radius, float length, Material skin)
        {
            // 碰撞体节点不缩放，可见胶囊挂在它下面缩放，避免碰撞体继承非等比缩放。
            var seg = new GameObject(name);
            seg.transform.SetParent(parent, false);
            seg.transform.localPosition = pos;
            seg.transform.localEulerAngles = euler;
            var col = seg.AddComponent<MagicaCapsuleCollider>();
            col.direction = MagicaCapsuleCollider.Direction.Y;
            col.SetSize(radius, radius, length);
            col.UpdateParameters();
            body.colliders.Add(col);
            body.capsules.Add(new CapsuleInfo { transform = seg.transform, radius = radius, length = length });

            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = name + "_Visual";
            Object.DestroyImmediate(vis.GetComponent<Collider>());
            vis.transform.SetParent(seg.transform, false);
            vis.transform.localScale = new Vector3(radius * 2f, length * 0.5f, radius * 2f);
            if (skin != null) vis.GetComponent<MeshRenderer>().sharedMaterial = skin;
            return seg.transform;
        }

        /// <summary>点到最近胶囊表面的有符号距离（负数表示在身体里）。</summary>
        public float SignedDistance(Vector3 world)
        {
            float best = float.MaxValue;
            foreach (var c in capsules)
            {
                var t = c.transform;
                Vector3 axis = t.up * (Mathf.Max(0f, c.length * 0.5f - c.radius));
                Vector3 a = t.position - axis, b = t.position + axis;
                Vector3 ab = b - a;
                float k = ab.sqrMagnitude < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(world - a, ab) / ab.sqrMagnitude);
                float d = Vector3.Distance(world, a + ab * k) - c.radius;
                if (d < best) best = d;
            }
            return best;
        }
    }
}
