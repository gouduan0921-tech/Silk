using System.Collections.Generic;
using UnityEngine;

namespace HuaShang.Greybox
{
    /// <summary>灰盒道具：旧而干净的木、中性灰米墙、细孔陶（风格规范「色与材质」）。不做塑料高光与彩色壁画。</summary>
    public static class Props
    {
        static readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();

        public static readonly Color Wood = new Color(0.35f, 0.27f, 0.21f);
        public static readonly Color WoodLight = new Color(0.48f, 0.38f, 0.29f);
        public static readonly Color Wall = new Color(0.72f, 0.70f, 0.66f);
        public static readonly Color Floor = new Color(0.46f, 0.44f, 0.41f);
        public static readonly Color Pottery = new Color(0.45f, 0.40f, 0.35f);
        public static readonly Color Warp = new Color(0.93f, 0.90f, 0.84f);

        public static Material Mat(Color c, float smooth = 0.15f)
        {
            int key = c.GetHashCode() ^ smooth.GetHashCode();
            if (cache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            m.SetFloat("_Smoothness", smooth);
            cache[key] = m;
            return m;
        }

        public static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Color c, bool keepCollider = false)
        {
            return Prim(PrimitiveType.Cube, parent, name, pos, size, c, keepCollider);
        }

        public static GameObject Cyl(Transform parent, string name, Vector3 pos, Vector3 size, Color c, bool keepCollider = false)
        {
            return Prim(PrimitiveType.Cylinder, parent, name, pos, size, c, keepCollider);
        }

        public static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 size, Color c, bool keepCollider)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = Mat(c);
            return go;
        }

        public static Transform Pose(Transform parent, string name, Vector3 pos, Vector3 lookAt)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.rotation = Quaternion.LookRotation(parent.TransformPoint(lookAt) - t.position, Vector3.up);
            return t;
        }

        /// <summary>一块平铺的布（静态网格 + 材质色），用于织机、染缸晾杆、裁桌。不挂 MeshCloth（docs/03 §1）。</summary>
        public static Renderer Cloth(Transform parent, string name, Vector3 pos, Vector3 euler, Vector2 size, Color c)
        {
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = name;
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(parent, false);
            q.transform.localPosition = pos;
            q.transform.localEulerAngles = euler;
            q.transform.localScale = new Vector3(size.x, size.y, 1);
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = c };
            m.SetFloat("_Cull", 0f);
            m.SetFloat("_Smoothness", 0.25f);
            var r = q.GetComponent<MeshRenderer>();
            r.sharedMaterial = m;
            return r;
        }

        public static void SetColor(Renderer r, Color c)
        {
            if (r == null) return;
            r.sharedMaterial.color = c;
        }
    }
}
