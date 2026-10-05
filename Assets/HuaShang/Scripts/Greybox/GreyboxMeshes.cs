using UnityEngine;

namespace HuaShang.Greybox
{
    /// <summary>
    /// 灰盒阶段的程序网格。正式服装用 Blender FBX（docs/29 §4）；这里只为 P1 探针与工位链占位。
    /// 约定：UV 的 v = 1 是固定边（领口、腰头、袖山），v = 0 是自由边。
    /// </summary>
    public static class GreyboxMeshes
    {
        /// <summary>上口半径 topRadius、下口半径 bottomRadius 的开口筒（裙、衣身、袖）。轴沿 -Y。</summary>
        /// <summary>
        /// MagicaCloth 2 读绘制图时把 uv 取模到 [0,1)，uv 恰为 1 的上边会读到图的最底行（可动），
        /// 上边就不固定了。灰盒网格的 uv 上限略小于 1。
        /// </summary>
        const float MaxUv = 0.999f;

        public static Mesh Tube(string name, float topRadius, float bottomRadius, float length, int around = 24, int down = 16)
        {
            var mesh = new Mesh { name = name };
            int cols = around + 1, rows = down + 1;
            var v = new Vector3[cols * rows];
            var uv = new Vector2[cols * rows];
            for (int r = 0; r < rows; r++)
            {
                float t = r / (float)down;
                float radius = Mathf.Lerp(topRadius, bottomRadius, t);
                for (int c = 0; c < cols; c++)
                {
                    float a = c / (float)around * Mathf.PI * 2f;
                    v[r * cols + c] = new Vector3(Mathf.Cos(a) * radius, -t * length, -Mathf.Sin(a) * radius); // 逆向绕行，使面与法线朝外
                    uv[r * cols + c] = new Vector2(Mathf.Min(c / (float)around, MaxUv), Mathf.Min(1f - t, MaxUv));
                }
            }
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = Grid(cols, rows);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>一块垂挂的平布，上边固定。中心在上边中点，向 -Y 垂下，面朝 +Z。</summary>
        public static Mesh Panel(string name, float width, float length, int across = 16, int down = 20)
        {
            var mesh = new Mesh { name = name };
            int cols = across + 1, rows = down + 1;
            var v = new Vector3[cols * rows];
            var uv = new Vector2[cols * rows];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float x = (c / (float)across - 0.5f) * width;
                    float y = -r / (float)down * length;
                    v[r * cols + c] = new Vector3(x, y, 0f);
                    uv[r * cols + c] = new Vector2(Mathf.Min(c / (float)across, MaxUv), Mathf.Min(1f - r / (float)down, MaxUv));
                }
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = Grid(cols, rows);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static int[] Grid(int cols, int rows)
        {
            var tris = new int[(cols - 1) * (rows - 1) * 6];
            int i = 0;
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cols - 1; c++)
                {
                    int a = r * cols + c, b = a + 1, d = a + cols, e = d + 1;
                    tris[i++] = a; tris[i++] = d; tris[i++] = b;
                    tris[i++] = b; tris[i++] = d; tris[i++] = e;
                }
            return tris;
        }

        /// <summary>MagicaCloth 2 的绘制图：v ≥ 1 − fixedFraction 的区域为红（固定），其余为绿（可动）。</summary>
        public static Texture2D PaintMap(float fixedFraction, int height = 64)
        {
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false, true) { name = "T_paint_fixedTop", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[4 * height];
            for (int y = 0; y < height; y++)
            {
                bool fixedRow = (y + 0.5f) / height >= 1f - fixedFraction;
                var col = fixedRow ? new Color32(255, 0, 0, 255) : new Color32(0, 255, 0, 255);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = col;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
