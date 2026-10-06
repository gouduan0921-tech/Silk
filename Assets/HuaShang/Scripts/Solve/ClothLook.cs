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
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        /// <summary>
        /// 防染纹样（docs/04 §5）：取最后一个防染层，纹样内露出「去掉该层」的颜色。
        /// 贴图存的是两色的线性比值（半精度，可大于 1），与 _BaseColor（整匹颜色）相乘；
        /// 这样材质颜色被重写时纹样仍然对。没有防染层返回 null。
        /// </summary>
        public static Texture2D ResistTexture(IList<DyeLayer> layers, DyeData dye, out string kind)
        {
            kind = null;
            if (layers == null) return null;
            int idx = -1;
            for (int i = layers.Count - 1; i >= 0; i--) if (!string.IsNullOrEmpty(layers[i].resist)) { idx = i; break; }
            if (idx < 0) return null;
            kind = layers[idx].resist;
            var without = new List<DyeLayer>(layers);
            without.RemoveAt(idx);
            Color top = DyedColor(layers, dye).linear, under = DyedColor(without, dye).linear;
            Color ratio = new Color(under.r / Mathf.Max(top.r, 1e-3f), under.g / Mathf.Max(top.g, 1e-3f), under.b / Mathf.Max(top.b, 1e-3f), 1f);
            const int N = 64;
            var tex = new Texture2D(N, N, TextureFormat.RGBAHalf, false, true) { name = "T_resist_" + kind, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = (x + 0.5f) / N - 0.5f, v = (y + 0.5f) / N - 0.5f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float m;
                    if (kind == "clamp")
                    {
                        // 夹缬：对称四瓣团花加花心
                        float ang = Mathf.Atan2(v, u);
                        float petal = 0.22f + 0.12f * Mathf.Abs(Mathf.Cos(2f * ang));
                        m = Mathf.Clamp01((petal - r) / 0.025f) * (r > 0.07f ? 1f : 0f) + (r < 0.04f ? 1f : 0f);
                    }
                    else
                    {
                        // 扎染：晕圈与中心点，边缘发虚
                        float ring = 1f - Mathf.Clamp01(Mathf.Abs(r - 0.28f) / 0.05f);
                        float dot = Mathf.Clamp01((0.08f - r) / 0.04f);
                        m = Mathf.Max(ring, dot) * (0.8f + 0.2f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f));
                    }
                    m = Mathf.Clamp01(m);
                    px[y * N + x] = Color.Lerp(Color.white, ratio, m);
                }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        static readonly int DetailAlbedoId = Shader.PropertyToID("_DetailAlbedoMap");
        static readonly int DetailNormalId = Shader.PropertyToID("_DetailNormalMap");
        static readonly int DetailNormalScaleId = Shader.PropertyToID("_DetailNormalMapScale");
        static Texture2D neutralDetail;

        /// <summary>
        /// 面料结构法线（docs/17 §4）：走 URP Lit 的细节法线，单独平铺得很密，不占主贴图的平铺（防染纹样用主贴图）。
        /// 细节反照率用中性灰，不改颜色。没有贴图时什么都不做。
        /// </summary>
        public static void ApplyFabric(Material m, Texture2D normal)
        {
            if (m == null || normal == null) return;
            if (neutralDetail == null) neutralDetail = Resources.Load<Texture2D>("HuaShang/Materials/T_detail_neutral");
            m.SetTexture(DetailNormalId, normal);
            m.SetFloat(DetailNormalScaleId, 0.8f);
            if (neutralDetail != null) m.SetTexture(DetailAlbedoId, neutralDetail);
            m.SetTextureScale(DetailAlbedoId, new Vector2(40f, 60f)); // 细节贴图共用这一组平铺
            m.EnableKeyword("_DETAIL_MULX2");
        }

        public static void ApplyResist(Material m, Texture2D tex, string kind)
        {
            if (m == null || tex == null) return;
            m.SetTexture(BaseMapId, tex);
            m.SetTextureScale(BaseMapId, kind == "clamp" ? new Vector2(4f, 6f) : new Vector2(6f, 9f));
        }

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
