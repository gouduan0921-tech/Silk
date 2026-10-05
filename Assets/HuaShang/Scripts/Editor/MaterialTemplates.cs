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
            AssetDatabase.SaveAssets();
            Debug.Log("[HuaShang] 运行时材质模板已生成：" + Dir);
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
