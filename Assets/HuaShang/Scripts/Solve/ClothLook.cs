using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;

namespace HuaShang.Solve
{
    /// <summary>
    /// 布的外观：颜色由底色与染层按 docs/04 §5 叠出；透明度与光泽按 docs/03 §4 换算。
    /// 只写 URP 材质实例，不写布料解算。
    /// </summary>
    public static class ClothLook
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        static readonly int BlendId = Shader.PropertyToID("_Blend");
        static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
        static readonly int CullId = Shader.PropertyToID("_Cull");

        /// <summary>按染层逐层叠色（sRGB 空间，与 docs/04 §5 的写法一致）。</summary>
        public static Color DyedColor(IList<DyeLayer> layers, DyeData dye)
        {
            var baseColor = dye.ColorOf("base");
            double r = baseColor != null ? baseColor.r : 1, g = baseColor != null ? baseColor.g : 1, b = baseColor != null ? baseColor.b : 1;
            if (layers != null)
            {
                foreach (var l in layers)
                {
                    var c = dye.ColorOf(l.dyeId);
                    if (c == null) continue; // 媒染不着色
                    r = DyeCalc.MixChannel(r, c.r, l.strength, dye);
                    g = DyeCalc.MixChannel(g, c.g, l.strength, dye);
                    b = DyeCalc.MixChannel(b, c.b, l.strength, dye);
                }
            }
            return new Color((float)r, (float)g, (float)b, 1f);
        }

        /// <summary>材质不透明度 α：透明度（1–10）先乘染色的透明度系数，再按 docs/03 §4 换算（平方）。</summary>
        public static float Alpha(ClothDescriptor d, ClothFixed f)
        {
            double transparency = d.opacity * d.opacityFactor;
            return Mathf.Clamp01((float)(1 - transparency * transparency * f.alphaPerOpacity));
        }

        public static float Smoothness(ClothDescriptor d, ClothFixed f) => Mathf.Clamp01((float)(d.gloss * f.smoothnessPerGloss));

        /// <summary>新建一个双面 URP Lit 材质实例。</summary>
        public static Material CreateMaterial()
        {
            // 从模板复制：透明变体要在构建里（LitMaterials）
            var m = HuaShang.Greybox.LitMaterials.New(HuaShang.Greybox.LitMaterials.Kind.Transparent, Color.white);
            m.name = "M_cloth_instance";
            m.SetFloat(CullId, 0f);
            return m;
        }

        public static void Write(Material m, Color dyed, float alpha, float smoothness)
        {
            dyed.a = alpha;
            m.SetColor(BaseColorId, dyed);
            m.SetFloat(SmoothnessId, smoothness);
            bool transparent = alpha < 0.999f;
            m.SetFloat(SurfaceId, transparent ? 1f : 0f);
            m.SetFloat(BlendId, 0f);
            m.SetFloat(SrcBlendId, transparent ? (float)UnityEngine.Rendering.BlendMode.SrcAlpha : (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat(DstBlendId, transparent ? (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha : (float)UnityEngine.Rendering.BlendMode.Zero);
            m.SetFloat(ZWriteId, transparent ? 0f : 1f);
            if (transparent) m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            else m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = transparent ? (int)UnityEngine.Rendering.RenderQueue.Transparent : (int)UnityEngine.Rendering.RenderQueue.Geometry;
            m.SetOverrideTag("RenderType", transparent ? "Transparent" : "Opaque");
        }

        public static Color ReadColor(Material m) => m.GetColor(BaseColorId);

        static readonly Dictionary<int, Material> prepassByQueue = new Dictionary<int, Material>();
        static Shader prepassShader;

        /// <summary>透明布按层排队：里层先画、外层后画。每层先写深度再画颜色。</summary>
        public const int LayerQueueBase = (int)UnityEngine.Rendering.RenderQueue.Transparent - 20;

        /// <summary>
        /// 把材质挂到渲染器上。半透明时同一子网格再画一次深度预写，并按层排队：
        /// order 0 是最里层，越大越靠外；每层先写深度、再混合颜色。
        /// 这样双面布只混合离镜头最近的一面，外层的素纱仍能透出已画好的里层。
        /// 不透明时只挂主材质。
        /// </summary>
        public static void Bind(Renderer r, Material main, int order)
        {
            if (r == null || main == null) return;
            bool transparent = main.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT");
            if (!transparent)
            {
                if (r.sharedMaterials.Length != 1 || r.sharedMaterial != main) r.sharedMaterials = new[] { main };
                return;
            }
            order = Mathf.Clamp(order, 0, 9);
            int prepassQueue = LayerQueueBase + order * 2;
            main.renderQueue = prepassQueue + 1;
            if (!prepassByQueue.TryGetValue(prepassQueue, out var pre) || pre == null)
            {
                if (prepassShader == null) prepassShader = Resources.Load<Shader>("HuaShang/ClothDepthPrepass");
                if (prepassShader == null) { r.sharedMaterials = new[] { main }; return; }
                pre = new Material(prepassShader) { name = "M_cloth_depth_prepass_" + order, renderQueue = prepassQueue };
                prepassByQueue[prepassQueue] = pre;
            }
            var have = r.sharedMaterials;
            if (have.Length == 2 && have[0] == main && have[1] == pre) return;
            r.sharedMaterials = new[] { main, pre };
        }
    }
}
