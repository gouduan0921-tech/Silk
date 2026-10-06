using System.IO;
using UnityEditor;
using UnityEngine;

namespace HuaShang.EditorTools
{
    /// <summary>
    /// 面料结构法线（docs/17 §4）：按组织结构程序生成可平铺的法线贴图，放进 ArtLibrary 的 Fabrics 目录。
    /// 平纹、纱（稀疏平纹）、罗（绞经孔眼）、斜纹（绫）、缎（长浮）、锦（缎地加纬浮花）、缂丝（平纹加断纬竖缝）。
    /// 美术交付 Krita 绘制的贴图后，同名覆盖即可。
    /// </summary>
    public static class FabricTextures
    {
        const string Dir = "Assets/HuaShang/Resources/HuaShang/Art/Fabrics";
        const int Size = 256;
        const int Threads = 16; // 每张贴图经纬各 16 根

        [MenuItem("HuaShang/美术/生成面料结构法线")]
        public static void GenerateAll()
        {
            Directory.CreateDirectory(Dir);
            foreach (var (id, kind) in new[]
            {
                ("suSha", "gauze"), ("juan", "plain"), ("chou", "plainDense"), ("keSi", "kesi"),
                ("huaSha", "leno"), ("suLuo", "leno"), ("huaLuo", "leno"),
                ("ling", "twill"), ("suDuan", "satin"), ("huaDuan", "satin"),
                ("puTongJin", "brocade"), ("shuJin", "brocade"), ("songJin", "brocade"), ("yunJin", "brocade"),
                ("zhuangHuaDuan", "brocade"), ("gaiJi", "brocade"),
            })
                Write(id, kind);
            AssetDatabase.Refresh();
            foreach (var f in Directory.GetFiles(Dir, "*.png"))
            {
                var imp = (TextureImporter)AssetImporter.GetAtPath(f.Replace('\\', '/'));
                if (imp == null) continue;
                imp.textureType = TextureImporterType.NormalMap;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            Debug.Log("[HuaShang] 面料结构法线已生成：" + Dir);
        }

        static void Write(string id, string kind)
        {
            var h = new float[Size, Size];
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                    h[x, y] = Height(kind, x / (float)Size * Threads, y / (float)Size * Threads);
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false, true);
            float strength = kind == "gauze" || kind == "leno" ? 2.5f : 4f;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = h[(x + 1) % Size, y] - h[(x - 1 + Size) % Size, y];
                    float dy = h[x, (y + 1) % Size] - h[x, (y - 1 + Size) % Size];
                    var n = new Vector3(-dx * strength, -dy * strength, 1f).normalized;
                    tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f));
                }
            File.WriteAllBytes(Path.Combine(Dir, "T_fabric_" + id + "_n.png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        /// <summary>一根线的截面：中间高、两边落下。f 是线内位置 0–1。</summary>
        static float Thread(float f, float width) { float d = Mathf.Abs(f - 0.5f) / (0.5f * width); return d >= 1f ? 0f : Mathf.Sqrt(1f - d * d); }

        static float Height(string kind, float u, float v)
        {
            int i = Mathf.FloorToInt(u), j = Mathf.FloorToInt(v);
            float fu = u - i, fv = v - j;
            switch (kind)
            {
                case "gauze": return Interlace(i, j, fu, fv, 0.55f, (i + j) % 2 == 0);
                case "plain": return Interlace(i, j, fu, fv, 0.85f, (i + j) % 2 == 0);
                case "plainDense": return Interlace(i, j, fu, fv, 0.95f, (i + j) % 2 == 0);
                case "kesi":
                    // 平纹加断纬：每隔几根经线有一道竖缝
                    float k = Interlace(i, j, fu, fv, 0.9f, (i + j) % 2 == 0);
                    return (i % 6 == 5 && fu > 0.75f) ? 0f : k;
                case "twill": return Interlace(i, j, fu, fv, 0.9f, ((i - j) % 3 + 3) % 3 != 0); // 2/1 斜纹
                case "satin": return Float(i, j, fu, fv, 5, 2);
                case "brocade":
                    // 缎地，再加成团的纬浮花
                    float baseH = Float(i, j, fu, fv, 5, 2);
                    float cx = u / Threads - 0.5f, cy = v / Threads - 0.5f;
                    bool motif = Mathf.Abs(cx) + Mathf.Abs(cy) < 0.28f && Mathf.Abs(cx) + Mathf.Abs(cy) > 0.12f;
                    return motif ? 0.6f + 0.4f * Thread(fv, 0.9f) : baseH;
                case "leno":
                    // 绞经：成对经线交叉，之间留孔
                    float pair = Thread(fu, 0.5f) * (1f - 0.6f * Mathf.Abs(Mathf.Sin(fv * Mathf.PI * 2f)));
                    float weft = (j % 2 == 0) ? Thread(fv, 0.4f) * 0.7f : 0f;
                    return Mathf.Max(pair, weft);
            }
            return 0f;
        }

        /// <summary>平纹交织：warpOver 时经线在上。</summary>
        static float Interlace(int i, int j, float fu, float fv, float width, bool warpOver)
        {
            float warp = Thread(fu, width), weft = Thread(fv, width);
            return warpOver ? Mathf.Max(warp, weft * 0.6f) : Mathf.Max(weft, warp * 0.6f);
        }

        /// <summary>缎纹：n 枚，步数 step；经线大多浮在上面，只在交织点下沉。</summary>
        static float Float(int i, int j, float fu, float fv, int n, int step)
        {
            bool tie = ((i * step - j) % n + n) % n == 0;
            float warp = Thread(fu, 0.98f);
            return tie ? warp * 0.35f + Thread(fv, 0.8f) * 0.5f : warp;
        }
    }
}
