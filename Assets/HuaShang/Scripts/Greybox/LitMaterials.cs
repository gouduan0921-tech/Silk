using UnityEngine;

namespace HuaShang.Greybox
{
    /// <summary>
    /// 运行时新建材质一律从 Resources 里的模板复制，不用 Shader.Find：
    /// 打包时 Unity 只保留构建里材质用到的着色器与关键字组合，运行时才打开的透明、关反射等变体会被剔掉。
    /// 模板由菜单「HuaShang/生成运行时材质模板」生成（MaterialTemplates.cs），随版本库提交。
    /// </summary>
    public static class LitMaterials
    {
        public enum Kind { Opaque, Transparent, Glass, Unlit }

        public const string Folder = "HuaShang/Materials/";

        public static string NameOf(Kind k)
        {
            switch (k)
            {
                case Kind.Transparent: return "M_template_lit_transparent";
                case Kind.Glass: return "M_template_lit_glass";
                case Kind.Unlit: return "M_template_unlit";
                default: return "M_template_lit_opaque";
            }
        }

        public static string ShaderOf(Kind k) => k == Kind.Unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit";

        public static Material New(Kind k, Color color)
        {
            var template = Resources.Load<Material>(Folder + NameOf(k));
            var m = template != null ? new Material(template) : new Material(Shader.Find(ShaderOf(k)));
            m.name = NameOf(k).Replace("template", "instance");
            m.color = color;
            return m;
        }
    }
}
