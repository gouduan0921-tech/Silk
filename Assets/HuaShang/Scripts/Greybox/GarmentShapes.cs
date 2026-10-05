using System.Collections.Generic;
using UnityEngine;

namespace HuaShang.Greybox
{
    /// <summary>
    /// 灰盒服装形体：汉风襦裙的上襦（衣身加两只宽袖）、带褶的裙、衬里、搭在背后与小臂上的披帛。
    /// 网格在人体根节点的局部空间里生成，与 GreyboxBody 的关节点对齐。
    /// 绘制图只看 uv.v：v 取 0.999 的顶点固定（肩线、袖山、披帛背中），其余 v ≤ 0.9 可动；
    /// 与 GreyboxMeshes.PaintMap(0.08) 配用。正式服装 FBX 交付后整体替换（docs/17）。
    /// </summary>
    public static class GarmentShapes
    {
        const float Fixed = 0.999f;
        const float FreeMax = 0.9f;

        public static Mesh Upper(string name)
        {
            var b = new Builder(name);
            // 衣身：肩线到腰下，左右宽、前后窄，下摆略放
            var spine = new List<Vector3> { new Vector3(0, 1.47f, 0), new Vector3(0, 1.30f, 0), new Vector3(0, 1.12f, 0), new Vector3(0, 0.98f, 0) };
            b.Sweep(spine, new[] { 0.16f, 0.185f, 0.21f, 0.25f }, 28, new Vector2(1.25f, 0.9f), 1, null);
            // 两只宽袖：袖山固定在肩，沿上臂、小臂走，袖口垂宽
            foreach (int side in new[] { -1, 1 })
            {
                var arm = GreyboxBody.ArmPoints(side);
                var path = new List<Vector3> { arm[0] + new Vector3(side * 0.02f, 0.02f, 0), arm[1], arm[2], arm[2] + (arm[2] - arm[1]).normalized * 0.08f };
                b.Sweep(path, new[] { 0.075f, 0.11f, 0.17f, 0.2f }, 18, Vector2.one, 1, null);
            }
            return b.Finish();
        }

        /// <summary>直裾：交领长袍，肩到地，窄袖到腕（docs/07 §3）。</summary>
        public static Mesh Robe(string name)
        {
            var b = new Builder(name);
            var spine = new List<Vector3> { new Vector3(0, 1.47f, 0), new Vector3(0, 1.25f, 0), new Vector3(0, 1.0f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.05f, 0) };
            b.Sweep(spine, new[] { 0.16f, 0.19f, 0.22f, 0.28f, 0.34f }, 36, new Vector2(1.2f, 0.95f), 1,
                (angle, down) => 1f + 0.03f * down * Mathf.Sin(angle * 8f));
            foreach (int side in new[] { -1, 1 })
            {
                var arm = GreyboxBody.ArmPoints(side);
                var path = new List<Vector3> { arm[0] + new Vector3(side * 0.02f, 0.02f, 0), arm[1], arm[2] + (arm[2] - arm[1]).normalized * 0.04f };
                b.Sweep(path, new[] { 0.075f, 0.09f, 0.1f }, 16, Vector2.one, 1, null);
            }
            return b.Finish();
        }

        /// <summary>大袖衫：宽袖外衫，罩在裙外，衣长过膝，袖口垂得很低（docs/07 §3）。</summary>
        public static Mesh BigSleeve(string name)
        {
            var b = new Builder(name);
            var spine = new List<Vector3> { new Vector3(0, 1.47f, 0), new Vector3(0, 1.2f, 0), new Vector3(0, 0.9f, 0), new Vector3(0, 0.5f, 0) };
            b.Sweep(spine, new[] { 0.17f, 0.22f, 0.3f, 0.38f }, 36, new Vector2(1.2f, 0.95f), 1, null);
            foreach (int side in new[] { -1, 1 })
            {
                var arm = GreyboxBody.ArmPoints(side);
                var path = new List<Vector3> { arm[0] + new Vector3(side * 0.02f, 0.02f, 0), arm[1], arm[2], arm[2] + (arm[2] - arm[1]).normalized * 0.14f };
                b.Sweep(path, new[] { 0.08f, 0.16f, 0.26f, 0.32f }, 22, Vector2.one, 1, null);
            }
            return b.Finish();
        }

        /// <summary>褙子：对襟长衫，窄袖到腕，衣长过膝，罩在裙外（docs/07 §3）。</summary>
        public static Mesh Beizi(string name)
        {
            var b = new Builder(name);
            var spine = new List<Vector3> { new Vector3(0, 1.47f, 0), new Vector3(0, 1.2f, 0), new Vector3(0, 0.85f, 0), new Vector3(0, 0.38f, 0) };
            b.Sweep(spine, new[] { 0.165f, 0.21f, 0.29f, 0.37f }, 32, new Vector2(1.15f, 0.95f), 1, null);
            foreach (int side in new[] { -1, 1 })
            {
                var arm = GreyboxBody.ArmPoints(side);
                var path = new List<Vector3> { arm[0] + new Vector3(side * 0.02f, 0.02f, 0), arm[1], arm[2] + (arm[2] - arm[1]).normalized * 0.03f };
                b.Sweep(path, new[] { 0.07f, 0.085f, 0.095f }, 16, Vector2.one, 1, null);
            }
            return b.Finish();
        }

        /// <summary>袄：交领短袄，衣长到胯，袖身宽、袖口收窄（明，docs/07 §3）。</summary>
        public static Mesh Ao(string name)
        {
            var b = new Builder(name);
            var spine = new List<Vector3> { new Vector3(0, 1.47f, 0), new Vector3(0, 1.25f, 0), new Vector3(0, 1.0f, 0), new Vector3(0, 0.86f, 0) };
            b.Sweep(spine, new[] { 0.165f, 0.2f, 0.25f, 0.28f }, 28, new Vector2(1.2f, 0.95f), 1, null);
            foreach (int side in new[] { -1, 1 })
            {
                var arm = GreyboxBody.ArmPoints(side);
                var path = new List<Vector3> { arm[0] + new Vector3(side * 0.02f, 0.02f, 0), arm[1], arm[2] - (arm[2] - arm[1]).normalized * 0.04f, arm[2] + (arm[2] - arm[1]).normalized * 0.04f };
                b.Sweep(path, new[] { 0.08f, 0.14f, 0.15f, 0.07f }, 18, Vector2.one, 1, null);
            }
            return b.Finish();
        }

        public static Mesh Inner(string name)
        {
            var b = new Builder(name);
            var spine = new List<Vector3> { new Vector3(0, 1.45f, 0), new Vector3(0, 1.25f, 0), new Vector3(0, 1.02f, 0) };
            b.Sweep(spine, new[] { 0.15f, 0.17f, 0.2f }, 24, new Vector2(1.2f, 0.9f), 1, null);
            return b.Finish();
        }

        public static Mesh Skirt(string name)
        {
            var b = new Builder(name);
            var spine = new List<Vector3> { new Vector3(0, 1.12f, 0), new Vector3(0, 0.85f, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0.08f, 0) };
            // 褶：越往下越深，十二道
            b.Sweep(spine, new[] { 0.175f, 0.24f, 0.32f, 0.42f }, 48, new Vector2(1.1f, 1f), 1,
                (angle, down) => 1f + 0.07f * down * Mathf.Sin(angle * 12f));
            return b.Finish();
        }

        /// <summary>披帛：背中搭在肩后（固定），绕过两臂，两端从小臂前垂下。</summary>
        public static Mesh Drape(string name)
        {
            var la = GreyboxBody.ArmPoints(-1);
            var ra = GreyboxBody.ArmPoints(1);
            var ctrl = new List<Vector3>
            {
                new Vector3(la[2].x - 0.03f, 0.55f, la[2].z + 0.04f),
                la[2] + new Vector3(-0.02f, 0.06f, 0.02f),
                la[1] + new Vector3(-0.06f, 0.04f, -0.06f),
                new Vector3(-0.16f, 1.36f, -0.2f),
                new Vector3(0, 1.38f, -0.22f),
                new Vector3(0.16f, 1.36f, -0.2f),
                ra[1] + new Vector3(0.06f, 0.04f, -0.06f),
                ra[2] + new Vector3(0.02f, 0.06f, 0.02f),
                new Vector3(ra[2].x + 0.03f, 0.55f, ra[2].z + 0.04f),
            };
            var path = Builder.Smooth(ctrl, 0.035f);
            var b = new Builder(name);
            b.Ribbon(path, 0.2f, s => Mathf.Abs(s - 0.5f) < 0.1f ? Fixed : FreeMax * (1f - Mathf.Abs(s - 0.5f) * 1.6f));
            return b.Finish();
        }

        class Builder
        {
            readonly string name;
            readonly List<Vector3> v = new List<Vector3>();
            readonly List<Vector2> uv = new List<Vector2>();
            readonly List<int> tri = new List<int>();

            public Builder(string name) { this.name = name; }

            /// <summary>沿折线扫出一圈圈环。第 0 圈固定，其余可动。ellipse 是沿起始法线与副法线的缩放。</summary>
            public void Sweep(List<Vector3> ctrl, float[] ctrlRadii, int around, Vector2 ellipse, int fixedRings, System.Func<float, float, float> mod)
            {
                // 每段细分到约 4 厘米
                var path = new List<Vector3>();
                var radii = new List<float>();
                for (int i = 0; i < ctrl.Count - 1; i++)
                {
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(ctrl[i], ctrl[i + 1]) / 0.04f));
                    for (int k = 0; k < n; k++)
                    {
                        float f = k / (float)n;
                        path.Add(Vector3.Lerp(ctrl[i], ctrl[i + 1], f));
                        radii.Add(Mathf.Lerp(ctrlRadii[i], ctrlRadii[i + 1], f));
                    }
                }
                path.Add(ctrl[ctrl.Count - 1]);
                radii.Add(ctrlRadii[ctrlRadii.Length - 1]);

                int start = v.Count, rings = path.Count, cols = around + 1;
                Vector3 normal = Vector3.zero;
                for (int r = 0; r < rings; r++)
                {
                    Vector3 t = (path[Mathf.Min(r + 1, rings - 1)] - path[Mathf.Max(r - 1, 0)]).normalized;
                    if (r == 0)
                    {
                        normal = Vector3.ProjectOnPlane(Vector3.right, t);
                        if (normal.sqrMagnitude < 1e-4f) normal = Vector3.ProjectOnPlane(Vector3.forward, t);
                    }
                    else normal = Vector3.ProjectOnPlane(normal, t); // 平行移动，环不扭
                    normal.Normalize();
                    Vector3 bin = Vector3.Cross(t, normal).normalized;
                    float down = r / (float)(rings - 1);
                    float vv = r < fixedRings ? Fixed : FreeMax * (1f - down);
                    for (int c = 0; c < cols; c++)
                    {
                        float a = c / (float)around * Mathf.PI * 2f;
                        float m = mod != null && r >= fixedRings ? mod(a, down) : 1f;
                        Vector3 off = normal * (Mathf.Cos(a) * ellipse.x) + bin * (Mathf.Sin(a) * ellipse.y);
                        v.Add(path[r] + off * radii[r] * m);
                        uv.Add(new Vector2(Mathf.Min(c / (float)around, Fixed), vv));
                    }
                }
                int triStart = tri.Count;
                for (int r = 0; r < rings - 1; r++)
                    for (int c = 0; c < around; c++)
                    {
                        int a0 = start + r * cols + c, a1 = a0 + 1, b0 = a0 + cols, b1 = b0 + 1;
                        tri.Add(a0); tri.Add(b0); tri.Add(a1);
                        tri.Add(a1); tri.Add(b0); tri.Add(b1);
                    }
                // 让面朝外：看第一个三角形的法线是否背离中心线
                Vector3 p0 = v[tri[triStart]], p1 = v[tri[triStart + 1]], p2 = v[tri[triStart + 2]];
                Vector3 fn = Vector3.Cross(p1 - p0, p2 - p0);
                if (Vector3.Dot(fn, p0 - path[0]) < 0)
                    for (int i = triStart; i < tri.Count; i += 3) { int s = tri[i + 1]; tri[i + 1] = tri[i + 2]; tri[i + 2] = s; }
            }

            /// <summary>沿路径的带子，宽度方向尽量水平。vOf(s) 给出路径位置 s（0–1）处的 v。</summary>
            public void Ribbon(List<Vector3> path, float width, System.Func<float, float> vOf)
            {
                int start = v.Count;
                Vector3 prevW = Vector3.zero;
                for (int i = 0; i < path.Count; i++)
                {
                    Vector3 t = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                    Vector3 w = Vector3.Cross(t, Vector3.up);
                    if (w.sqrMagnitude < 0.09f) w = Vector3.Cross(t, Vector3.forward);
                    w.Normalize();
                    if (i > 0 && Vector3.Dot(w, prevW) < 0) w = -w; // 不翻面
                    prevW = w;
                    float s = i / (float)(path.Count - 1);
                    float vv = vOf(s);
                    v.Add(path[i] - w * width * 0.5f); uv.Add(new Vector2(0, vv));
                    v.Add(path[i] + w * width * 0.5f); uv.Add(new Vector2(Fixed, vv));
                }
                for (int i = 0; i < path.Count - 1; i++)
                {
                    int a = start + i * 2, b = a + 1, c = a + 2, d = a + 3;
                    tri.Add(a); tri.Add(c); tri.Add(b);
                    tri.Add(b); tri.Add(c); tri.Add(d);
                }
            }

            public static List<Vector3> Smooth(List<Vector3> ctrl, float step)
            {
                var o = new List<Vector3>();
                for (int i = 0; i < ctrl.Count - 1; i++)
                {
                    Vector3 p0 = ctrl[Mathf.Max(i - 1, 0)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(i + 2, ctrl.Count - 1)];
                    int n = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / step));
                    for (int k = 0; k < n; k++)
                    {
                        float t = k / (float)n, t2 = t * t, t3 = t2 * t;
                        o.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
                    }
                }
                o.Add(ctrl[ctrl.Count - 1]);
                return o;
            }

            public Mesh Finish()
            {
                var m = new Mesh { name = name };
                m.SetVertices(v);
                m.SetUVs(0, uv);
                m.SetTriangles(tri, 0);
                m.RecalculateNormals();
                m.RecalculateBounds();
                return m;
            }
        }
    }
}
