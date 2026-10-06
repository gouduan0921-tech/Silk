using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HuaShang.EditorTools
{
    /// <summary>
    /// 正式美术的材质库（docs/17 §7）：Blender 导出的模型只带材质名（tools/blender/hs_art.py），
    /// 这里按名字提供 URP 材质与程序贴图（木纹、灰泥、纸、陶、竹、石），并把模型的同名材质重映射过来。
    /// </summary>
    public static class ArtMaterials
    {
        const string MatDir = "Assets/HuaShang/Art/Materials";
        const string TexDir = "Assets/HuaShang/Art/Textures";
        const string ModelRoot = "Assets/HuaShang/Resources/HuaShang/Art";

        struct Spec { public string name, tex; public Color color; public float smooth, metal; public Vector2 tiling; }

        static readonly Spec[] Specs =
        {
            S("M_wood", "T_wood", new Color(0.62f, 0.45f, 0.31f), 0.25f, 0, 2f),
            S("M_wood_light", "T_wood", new Color(0.86f, 0.70f, 0.52f), 0.3f, 0, 2f),
            S("M_wood_dark", "T_wood", new Color(0.42f, 0.29f, 0.20f), 0.25f, 0, 2f),
            S("M_lacquer_red", "T_noise", new Color(0.52f, 0.12f, 0.08f), 0.6f, 0, 1f),
            S("M_plaster", "T_plaster", new Color(0.86f, 0.82f, 0.74f), 0.05f, 0, 1f),
            S("M_paper", "T_paper", new Color(0.93f, 0.90f, 0.83f), 0.1f, 0, 1f),
            S("M_pottery", "T_pottery", new Color(0.40f, 0.33f, 0.28f), 0.55f, 0, 1f),
            S("M_bamboo", "T_bamboo", new Color(0.78f, 0.69f, 0.45f), 0.4f, 0, 2f),
            S("M_stone", "T_plaster", new Color(0.55f, 0.54f, 0.51f), 0.15f, 0, 1f),
            S("M_brass", "T_noise", new Color(0.78f, 0.62f, 0.35f), 0.65f, 0.85f, 1f),
            S("M_thread", null, new Color(0.93f, 0.91f, 0.86f), 0.3f, 0, 1f),
            S("M_ink", "T_paper", new Color(0.28f, 0.29f, 0.30f), 0.05f, 0, 1f),
            S("M_tile", "T_noise", new Color(0.30f, 0.31f, 0.33f), 0.35f, 0, 1f),
            S("M_skin", null, new Color(0.90f, 0.76f, 0.66f), 0.35f, 0, 1f),
            S("M_hair", null, new Color(0.06f, 0.05f, 0.05f), 0.55f, 0, 1f),
            S("M_hair_brown", null, new Color(0.30f, 0.19f, 0.10f), 0.55f, 0, 1f),
            S("M_hair_light", null, new Color(0.52f, 0.38f, 0.22f), 0.55f, 0, 1f),
            S("M_gold", "T_noise", new Color(0.85f, 0.68f, 0.32f), 0.7f, 0.9f, 1f),
            S("M_jade", null, new Color(0.55f, 0.72f, 0.60f), 0.75f, 0, 1f),
            S("M_cloth_prop", "T_paper", new Color(0.78f, 0.72f, 0.62f), 0.15f, 0, 1f),
            S("M_ink_light", "T_paper", new Color(0.74f, 0.74f, 0.72f), 0.05f, 0, 1f),
            S("M_ink_mid", "T_paper", new Color(0.54f, 0.55f, 0.54f), 0.05f, 0, 1f),
            S("M_curtain", "T_paper", new Color(0.55f, 0.16f, 0.12f), 0.2f, 0, 1f),
            S("M_leaf", "T_noise", new Color(0.33f, 0.48f, 0.24f), 0.35f, 0, 1f),
        };

        static Spec S(string n, string t, Color c, float s, float m, float tile) => new Spec { name = n, tex = t, color = c, smooth = s, metal = m, tiling = Vector2.one * tile };

        [MenuItem("HuaShang/美术/更新材质库并重映射模型")]
        public static string Run()
        {
            Directory.CreateDirectory(MatDir);
            Directory.CreateDirectory(TexDir);
            var tex = new Dictionary<string, Texture2D>();
            foreach (var t in new[] { "T_wood", "T_noise", "T_plaster", "T_paper", "T_pottery", "T_bamboo" }) tex[t] = MakeTexture(t);
            var lib = new Dictionary<string, Material>();
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            foreach (var s in Specs)
            {
                string p = MatDir + "/" + s.name + ".mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(p);
                if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, p); }
                m.SetColor("_BaseColor", s.color);
                m.SetFloat("_Smoothness", s.smooth);
                m.SetFloat("_Metallic", s.metal);
                if (s.tex != null) { m.SetTexture("_BaseMap", tex[s.tex]); m.SetTextureScale("_BaseMap", s.tiling); }
                EditorUtility.SetDirty(m);
                lib[s.name] = m;
            }
            AssetDatabase.SaveAssets();
            int remapped = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { ModelRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.importAnimation = false;
                imp.isReadable = false;
                bool changed = false;
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (!(o is Material em)) continue;
                    string n = em.name;
                    if (!lib.TryGetValue(n, out var target)) continue;
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), target);
                    changed = true;
                }
                // 已重映射过的不再列出嵌入材质，按外部引用检查
                foreach (var kv in imp.GetExternalObjectMap())
                    if (kv.Key.type == typeof(Material) && lib.ContainsKey(kv.Key.name)) changed = true;
                if (changed) { imp.SaveAndReimport(); remapped++; }
            }
            return "材质 " + lib.Count + "，模型 " + remapped;
        }

        static Texture2D MakeTexture(string name)
        {
            string path = TexDir + "/" + name + ".png";
            const int N = 512;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float g = Gray(name, u, v);
                    t.SetPixel(x, y, new Color(g, g, g, 1));
                }
            t.Apply();
            File.WriteAllBytes(path, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>可平铺的灰度（乘在材质颜色上，1 附近）。</summary>
        static float Gray(string name, float u, float v)
        {
            float Tile(float a, float b, float s) => Mathf.PerlinNoise(Mathf.Sin(a * Mathf.PI * 2) * s + 50, Mathf.Sin(b * Mathf.PI * 2) * s + Mathf.Cos(a * Mathf.PI * 2) * s * 0.7f + 50);
            switch (name)
            {
                case "T_wood":
                {
                    // 年轮：沿 v 的条纹，被低频噪声扭曲
                    // 顺纹：沿 v 拉长的细纹，低频轻微摆动；再叠一层很细的纤维
                    float warp = Tile(u, v, 1.2f) * 1.2f;
                    float grain = Mathf.Sin((u * 40f + warp) * Mathf.PI * 2f) * 0.5f + 0.5f;
                    float fiber = Mathf.PerlinNoise(u * 180f, v * 6f);
                    return 0.82f + 0.1f * grain * grain + 0.08f * fiber;
                }
                case "T_bamboo":
                {
                    float node = Mathf.Abs(((v * 4f) % 1f) - 0.5f) < 0.02f ? 0.75f : 1f;
                    return node * (0.88f + 0.1f * Mathf.Sin(u * Mathf.PI * 2 * 30) * 0.5f + 0.06f * Tile(u, v, 4f));
                }
                case "T_plaster": return 0.93f + 0.07f * Tile(u, v, 7f) * Tile(v, u, 13f);
                case "T_paper": return 0.92f + 0.08f * Tile(u, v, 12f);
                case "T_pottery": return 0.88f + 0.08f * Tile(u, v, 6f) + 0.04f * Tile(v, u, 16f);
                default: return 0.9f + 0.1f * Tile(u, v, 5f);
            }
        }
    }
}
