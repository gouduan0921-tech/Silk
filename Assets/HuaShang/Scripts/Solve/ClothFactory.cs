using System.Collections.Generic;
using UnityEngine;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;

namespace HuaShang.Solve
{
    /// <summary>布匹字段 → cloth 描述与外观色。纯计算，由解算模块调用（构架文档 §2.2）。</summary>
    public static class ClothFactory
    {
        public static ClothDescriptor Describe(ConfigSnapshot c, string varietyId, double t, string dynasty,
                                               IList<DyeLayer> layers, string finish, int edgeDamage, double? sewScore)
            => ClothDescribe.FromLayers(c, varietyId, t, dynasty, layers, finish, edgeDamage, sewScore);

        public static Color Color(ConfigSnapshot c, IList<DyeLayer> layers) => ClothLook.DyedColor(layers, c.balance.dye);

        /// <summary>首发朝代：docs/04 §4「首发三种品种只用汉偏移」。</summary>
        public static string LaunchDynasty(ConfigSnapshot c)
        {
            var d = c.balance.quality.dynasties.Find(x => x.launch && x.parsed);
            return d != null ? d.dynasty : null;
        }
    }
}
