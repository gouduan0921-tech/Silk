using MagicaCloth2;
using HuaShang.Rules.Calc;
using HuaShang.Rules.Config;

namespace HuaShang.Solve
{
    /// <summary>
    /// 唯一把 cloth 描述写进 MagicaCloth 2 的地方（构架文档 §2.3）。
    /// 字段对照见 docs/03 §2；固定项来自 ClothFixed（docs/03 §2），不写字面量。
    /// 摩擦与按品种的迭代次数不写入。
    /// </summary>
    public static class ClothApplier
    {
        /// <summary>写入构建前的序列化数据（BuildAndRun 之前调用）。</summary>
        public static void WriteSerializeData(ClothSerializeData sdata, ClothDescriptor d, ClothFixed f)
        {
            sdata.gravity = (float)d.density;
            sdata.gravityFalloff = (float)f.gravityFalloff;
            sdata.damping.SetValue((float)d.damping);
            sdata.radius.SetValue((float)f.radius);
            sdata.angleRestorationConstraint.stiffness.SetValue((float)d.bend);
            sdata.triangleBendingConstraint.stiffness = (float)f.triangleBendingStiffness;
            sdata.distanceConstraint.stiffness.SetValue((float)d.stretch);
            sdata.wind.influence = (float)d.wind;
        }

        /// <summary>运行中改参数：写入后必须调用 SetParameterChange()。</summary>
        public static void Apply(MagicaCloth cloth, ClothDescriptor d, ClothFixed f)
        {
            if (cloth == null || d == null || f == null) return;
            WriteSerializeData(cloth.SerializeData, d, f);
            cloth.SetParameterChange();
        }
    }
}
