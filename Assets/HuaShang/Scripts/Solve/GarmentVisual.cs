using System.Collections.Generic;
using UnityEngine;
using HuaShang.Greybox;
using HuaShang.Play;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Solve
{
    /// <summary>
    /// 把成衣或一组衣片穿到灰盒人体上（人台、舞台、展柜共用同一份布料配置，docs/07 §5）。
    /// 每个部件一个 MeshCloth；颜色只来自底色与染层；呈现模式只改显示拷贝。
    /// 灰盒部件网格在西施与服装 FBX 交付后替换（docs/29 §4）。
    /// </summary>
    public class GarmentVisual : MonoBehaviour
    {
        public GreyboxBody body;
        public ClothContext context = ClothContext.Form;
        public string ownerId;
        public readonly List<ClothPart> parts = new List<ClothPart>();
        static Texture2D paint;

        public struct PartSpec
        {
            public string slot;
            public Bolt bolt;
            public double? sewScore;
            /// <summary>形制 id，决定 robe 的形体（直裾长袍或大袖）；为空按襦裙。</summary>
            public string pattern;
            /// <summary>成衣的朝代风格（形制决定）；为空用布匹自己的。</summary>
            public string dynasty;
        }

        public void Clear()
        {
            foreach (var p in parts) if (p != null) Destroy(p.gameObject);
            parts.Clear();
        }

        /// <param name="t">区间位置：成衣用成衣 Q 的 t，衣片阶段用布匹 Q 的 t。</param>
        /// <param name="presentMode">null 或 standard 为仓库原样；历史还原或视觉增强只改这份显示拷贝。</param>
        public void Build(ConfigSnapshot c, IList<PartSpec> specs, double t, bool layered, string presentMode = null)
        {
            Clear();
            if (paint == null) paint = GreyboxMeshes.PaintMap(0.08f);
            foreach (var sp in specs)
            {
                if (sp.bolt == null) continue;
                // 网格在人体根节点空间生成（GarmentShapes），部件直接挂在人体根上
                Mesh mesh; ClothLayer layer;
                switch (sp.slot)
                {
                    case "skirt": mesh = GarmentShapes.Skirt("SK_part_skirt"); layer = ClothLayer.Outer; break;
                    case "inner": mesh = GarmentShapes.Inner("SK_part_inner"); layer = ClothLayer.Inner; break;
                    case "drape": mesh = GarmentShapes.Drape("SK_part_drape"); layer = ClothLayer.Drape; break;
                    case "robe":
                        mesh = sp.pattern == "daXiuShan" ? GarmentShapes.BigSleeve("SK_part_daxiu") : GarmentShapes.Robe("SK_part_robe");
                        layer = ClothLayer.Outer; break;
                    default: mesh = GarmentShapes.Upper("SK_part_upper"); layer = ClothLayer.Outer; break;
                }
                var go = new GameObject("Part_" + sp.slot);
                go.transform.SetParent(body.transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                var part = go.AddComponent<ClothPart>();
                part.sourceRenderer = mr;
                part.layer = layer;
                part.context = context;
                part.ownerId = ownerId;
                part.layered = layered;
                var b = sp.bolt;
                var d = string.IsNullOrEmpty(presentMode) || presentMode == ClothDescribe.ModeStandard
                    ? ClothDescribe.FromLayers(c, b.variety, t, sp.dynasty ?? b.dynastyStyle, b.dyeLayers, b.finish, b.edgeDamage, sp.sewScore)
                    : ClothDescribe.ForPresentation(c, presentMode, b.variety, t, sp.dynasty ?? b.dynastyStyle, b.dyeLayers, b.finish, b.edgeDamage, sp.sewScore);
                part.Build(d, ClothLook.DyedColor(b.dyeLayers, c.balance.dye), c.clothFixed, paint, body.colliders, false);
                parts.Add(part);
            }
        }

        /// <summary>成衣：t 取成衣 Q（docs/03 §2 按品质档取 t）。</summary>
        public void BuildGarment(SaveRoot s, ConfigSnapshot c, Garment g, string presentMode = null)
        {
            var specs = new List<PartSpec>();
            foreach (var p in g.parts) specs.Add(new PartSpec { slot = p.slot, bolt = Play.Find.Bolt(s, p.boltId), sewScore = p.sewScore, pattern = g.pattern, dynasty = g.dynastyStyle });
            double t = ItemQuality.TryGarmentQ(s, c, g.parts, out var q, out _) ? QualityCalc.T(q, c.balance.quality) : 0;
            Build(c, specs, t, g.traits != null && g.traits.Contains("层叠"), presentMode ?? g.presentMode);
        }

        public void SetOuterVisible(bool on)
        {
            foreach (var p in parts)
                if (p.layer == ClothLayer.Outer || p.layer == ClothLayer.Drape) p.sourceRenderer.enabled = on;
        }
    }
}
