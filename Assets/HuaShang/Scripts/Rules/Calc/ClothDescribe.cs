using System;
using System.Collections.Generic;
using HuaShang.Rules.Config;

namespace HuaShang.Rules.Calc
{
    /// <summary>
    /// 布匹字段 → cloth 描述。纯函数，读档重算（S2）与解算模块共用。
    /// 呈现模式在显示拷贝上再夹一次（docs/03 §2、docs/04 §4），不写回仓库。
    /// </summary>
    public static class ClothDescribe
    {
        public const string Calender = "calender";
        public const string ModeHistory = "history";
        public const string ModeStandard = "standard";
        public const string ModeEnhanced = "enhanced";

        public static ClothDescriptor FromLayers(ConfigSnapshot c, string varietyId, double t, string dynasty,
                                                 IList<DyeLayer> layers, string finish, int edgeDamage, double? sewScore)
        {
            int plant = 0, strong = 0;
            bool vitriol = false;
            double sum = 0;
            if (layers != null)
            {
                foreach (var l in layers)
                {
                    var row = c.dyes.Find(d => d.id == l.dyeId);
                    if (row != null && row.role == "mordant") { vitriol = true; continue; }
                    plant++;
                    sum += l.strength;
                    if (l.concentration == DyeCalc.Strong) strong++;
                }
            }
            return ClothRecipe.Compute(new ClothInput
            {
                varietyId = varietyId,
                t = t,
                dynasty = dynasty,
                plantLayers = plant,
                strongLayers = strong,
                vitriol = vitriol,
                calendered = finish == Calender,
                averageStrength = plant > 0 ? sum / plant : 0,
                edgeDamage = edgeDamage,
                sewScore = sewScore,
            }, c);
        }

        /// <summary>呈现模式对 t 的限制（docs/04 §4）。标准模式不改。</summary>
        public static double ClampT(double t, string mode, QualityData q)
        {
            var p = q.presentation.Find(x => x.mode == mode);
            if (p == null) return t;
            if (p.hasTMax) t = Math.Min(t, p.tMax);
            if (p.hasTMin) t = Math.Max(t, p.tMin);
            return t;
        }

        /// <summary>
        /// 显示拷贝：先按模式夹 t 重算，再乘模式的光泽与薄纱风力系数，最后夹回该行（视觉增强也不能超出高端）。
        /// </summary>
        public static ClothDescriptor ForPresentation(ConfigSnapshot c, string mode, string varietyId, double t, string dynasty,
                                                      IList<DyeLayer> layers, string finish, int edgeDamage, double? sewScore)
        {
            var d = FromLayers(c, varietyId, ClampT(t, mode, c.balance.quality), dynasty, layers, finish, edgeDamage, sewScore);
            var p = c.balance.quality.presentation.Find(x => x.mode == mode);
            if (p == null) return d;
            var v = c.Variety(varietyId);
            d.gloss = Math.Min(v.glossMax, Math.Max(v.glossMin, d.gloss * p.glossFactor));
            if (v.group == "gauze")
                d.wind = Math.Min(v.windMax, Math.Max(v.windMin, d.wind * p.gauzeWindFactor));
            return d;
        }
    }
}
