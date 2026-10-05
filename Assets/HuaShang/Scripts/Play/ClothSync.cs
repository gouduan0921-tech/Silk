using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;
using HuaShang.Save;

namespace HuaShang.Play
{
    /// <summary>
    /// 读档后重算 cloth（docs/21 §4、docs/03 §2）。属于解算模块的职责：只有解算模块调用它（构架文档 §2.1）。
    /// 缺 cloth 时重算（S2）；clothLocked 存在时不覆盖（S3）。
    /// </summary>
    public static class ClothSync
    {
        public static ClothDescriptor Describe(SaveRoot s, ConfigSnapshot c, Bolt b, double? sewScore = null)
        {
            return ClothDescribe.FromLayers(c, b.variety, ItemQuality.BoltT(b, c), b.dynastyStyle, b.dyeLayers, b.finish, b.edgeDamage, sewScore);
        }

        public static void RecomputeAll(SaveRoot s, ConfigSnapshot c)
        {
            foreach (var b in s.bolts)
            {
                if (b.clothLocked != null) continue;
                b.cloth = Describe(s, c, b);
            }
        }
    }
}
