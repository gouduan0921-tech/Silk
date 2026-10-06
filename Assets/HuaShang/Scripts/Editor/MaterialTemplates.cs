using UnityEditor;
using UnityEngine;
using HuaShang.Greybox;

namespace HuaShang.EditorTools
{
    /// <summary>生成 Resources 下的运行时材质模板，使打包保留透明、关环境反射等 URP 变体（LitMaterials）。</summary>
    public static class MaterialTemplates
    {
        const string Dir = "Assets/HuaShang/Resources/HuaShang/Materials";

        [MenuItem("HuaShang/生成运行时材质模板")]
        public static void Generate()
        {
            if (!AssetDatabase.IsValidFolder("Assets/HuaShang/Resources/HuaShang"))
                AssetDatabase.CreateFolder("Assets/HuaShang/Resources", "HuaShang");
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/HuaShang/Resources/HuaShang", "Materials");

            Make(LitMaterials.Kind.Opaque, m => { });
            Make(LitMaterials.Kind.Transparent, m => SetTransparent(m));
            Make(LitMaterials.Kind.Glass, m =>
            {
                SetTransparent(m);
                m.SetFloat("_EnvironmentReflections", 0f);
                m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            });
            Make(LitMaterials.Kind.Unlit, m => { });
            // 带结构法线的布料：保留细节贴图 _DETAIL_MULX2 变体（ClothLook.ApplyFabric 在运行时打开）
            string clothNormal = Dir + "/M_template_cloth_normal.mat";
            var cn = AssetDatabase.LoadAssetAtPath<Material>(clothNormal);
            if (cn == null) { cn = new Material(Shader.Find(LitMaterials.ShaderOf(LitMaterials.Kind.Transparent))); AssetDatabase.CreateAsset(cn, clothNormal); }
            SetTransparent(cn);
            cn.EnableKeyword("_DETAIL_MULX2");
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/HuaShang/Resources/HuaShang/Art/Fabrics/T_fabric_juan_n.png");
            var neutral = NeutralDetail();
            if (tex != null) cn.SetTexture("_DetailNormalMap", tex);
            cn.SetTexture("_DetailAlbedoMap", neutral);
            EditorUtility.SetDirty(cn);
            // 不透明也要一份
            string clothNormalOpaque = Dir + "/M_template_cloth_normal_opaque.mat";
            var co = AssetDatabase.LoadAssetAtPath<Material>(clothNormalOpaque);
            if (co == null) { co = new Material(Shader.Find(LitMaterials.ShaderOf(LitMaterials.Kind.Opaque))); AssetDatabase.CreateAsset(co, clothNormalOpaque); }
            co.EnableKeyword("_DETAIL_MULX2");
            if (tex != null) co.SetTexture("_DetailNormalMap", tex);
            co.SetTexture("_DetailAlbedoMap", neutral);
            EditorUtility.SetDirty(co);
            AssetDatabase.SaveAssets();
            Debug.Log("[HuaShang] 运行时材质模板已生成：" + Dir);
        }

        /// <summary>细节反照率的中性灰（sRGB 128，乘 2 后不改颜色）。</summary>
        static Texture2D NeutralDetail()
        {
            string path = Dir + "/T_detail_neutral.png";
            if (!System.IO.File.Exists(path))
            {
                var t = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                var px = new Color32[16];
                for (int i = 0; i < 16; i++) px[i] = new Color32(128, 128, 128, 255);
                t.SetPixels32(px);
                System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void SetTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
        }

        static void Make(LitMaterials.Kind k, System.Action<Material> setup)
        {
            string path = Dir + "/" + LitMaterials.NameOf(k) + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(LitMaterials.ShaderOf(k)));
                AssetDatabase.CreateAsset(m, path);
            }
            setup(m);
            EditorUtility.SetDirty(m);
        }
    }
}
