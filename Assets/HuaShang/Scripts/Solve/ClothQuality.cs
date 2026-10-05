namespace HuaShang.Solve
{
    /// <summary>docs/03 §3 的四档。</summary>
    public enum ClothQuality
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3,
    }

    /// <summary>服装部件的层位。披帛算外层附件（docs/04 §4）。</summary>
    public enum ClothLayer
    {
        Outer = 0,
        Middle = 1,
        Inner = 2,
        Drape = 3,
    }

    /// <summary>某一档下哪些层实时解算（docs/03 §3）。</summary>
    public static class ClothQualityRules
    {
        /// <param name="layered">成衣带「层叠」词条：中档仍保留外层和披帛。</param>
        public static bool Simulates(ClothQuality q, ClothLayer layer, bool layered)
        {
            switch (q)
            {
                case ClothQuality.Ultra: return true;
                case ClothQuality.High: return layer == ClothLayer.Outer || layer == ClothLayer.Drape;
                case ClothQuality.Medium:
                    return layer == ClothLayer.Outer || (layered && layer == ClothLayer.Drape);
                default: return false;
            }
        }

        public static bool SelfCollision(ClothQuality q) => q == ClothQuality.Ultra;
    }
}
