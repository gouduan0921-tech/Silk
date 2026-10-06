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

        /// <summary>手臂关节点（身体根节点局部空间）：肩、肘、腕。side −1 为左，+1 为右。双手在腹前，小臂略向前收。</summary>
        public static Vector3[] ArmPoints(int side)
        {
            return new[]
            {
                new Vector3(side * 0.19f, 1.40f, 0f),
                new Vector3(side * 0.27f, 1.13f, 0.02f),
                new Vector3(side * 0.17f, 0.93f, 0.17f),
            };
        }

        /// <summary>身高约 1.6 米，脚底在原点。arms 为 false 时不做手臂（P1 探针用筒形上襦验证穿模，留无臂）。</summary>
        GameObject art;
        string artFor;

        /// <summary>
        /// 换上正式角色外形（ArtLibrary.Character）；没有就显示灰盒。碰撞仍用灰盒胶囊，布料碰撞不受影响。
        /// </summary>
        public void ApplyCharacterArt(string characterId)
        {
            if (artFor == characterId) return;
            artFor = characterId;
            if (art != null) Destroy(art);
            art = null;
            var own = new List<Renderer>(GetComponentsInChildren<Renderer>());
            own.RemoveAll(r => r.GetComponentInParent<HuaShang.Solve.ClothPart>() != null);
            foreach (var r in own) r.enabled = true;
            art = ArtLibrary.Swap(ArtLibrary.Character(characterId), transform, own);
        }

        /// <summary>
        /// 人体本地朝 +Z（手臂前伸、披帛搭在 -Z 的肩后）。人台、戏台、展柜的镜头在 -Z 一侧，
        /// 所以人体挂在一个转了 180° 的节点下，正面对着观众。
        /// </summary>
        public static Transform FacingPivot(Transform parent)
        {
            var t = new GameObject("FacingPivot").transform;
            t.SetParent(parent, false);
            t.localRotation = Quaternion.Euler(0, 180f, 0);
            return t;
        }

        public static GreyboxBody Create(string name, Transform parent, Material skin, bool arms = true)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var body = root.AddComponent<GreyboxBody>();
            body.hips = Segment(body, root.transform, "Hips", new Vector3(0, 0.95f, 0), Vector3.zero, 0.15f, 0.25f, skin);
            body.torso = Segment(body, root.transform, "Torso", new Vector3(0, 1.25f, 0), Vector3.zero, 0.14f, 0.40f, skin);
            Segment(body, root.transform, "Head", new Vector3(0, 1.52f, 0), Vector3.zero, 0.09f, 0.20f, skin);
            body.leftLeg = Segment(body, root.transform, "LegL", new Vector3(-0.08f, 0.45f, 0), Vector3.zero, 0.07f, 0.85f, skin);
            body.rightLeg = Segment(body, root.transform, "LegR", new Vector3(0.08f, 0.45f, 0), Vector3.zero, 0.07f, 0.85f, skin);
            if (arms)
            {
                // 上襦带袖（GarmentShapes.Upper），手臂在袖里；袖靠臂上的碰撞体托住
                foreach (int side in new[] { -1, 1 })
                {
                    var p = ArmPoints(side);
                    string tag = side < 0 ? "L" : "R";
                    Between(body, root.transform, "UpperArm" + tag, p[0], p[1], 0.045f, skin);
                    Between(body, root.transform, "Forearm" + tag, p[1], p[2], 0.04f, skin);
                }
            }
            return body;
        }

        static Transform Between(GreyboxBody body, Transform parent, string name, Vector3 a, Vector3 b, float radius, Material skin)
        {
            var dir = b - a;
            var euler = Quaternion.FromToRotation(Vector3.up, dir.normalized).eulerAngles;
            return Segment(body, parent, name, (a + b) * 0.5f, euler, radius, dir.magnitude + radius * 2f, skin);
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
