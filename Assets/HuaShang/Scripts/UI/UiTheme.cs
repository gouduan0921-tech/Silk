using UnityEngine;

namespace HuaShang.UI
{
    /// <summary>
    /// 「一幅素纱」界面规格（UI设计/华裳3D界面/风格规范.md）。这些是界面规格，不是玩法参数。
    /// 1920×1080 与 1280×720 两档，其他分辨率按高度等比缩放。
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Paper = Hex(0xE8E2D4);      // 素纸
        public static readonly Color Ink = Hex(0x302E29);        // 墨灰
        public static readonly Color OldGrey = Hex(0x6C695D);    // 旧纸灰
        public static readonly Color Walnut = Hex(0x594637);     // 胡桃木
        public static readonly Color Indigo = Hex(0x3D5963);     // 靛蓝
        public static readonly Color PaperEdge = Hex(0xC5BDAC);  // 纸边
        public static readonly Color GauzeWhite = Hex(0xFAF6ED); // 纱白
        public static readonly Color Terracotta = Hex(0x855039); // 陶褐：警告线与文字，必须配图标
        public static readonly Color Selected = Hex(0xD2DDD9);   // 选中淡灰绿
        public static readonly Color Focus = Hex(0xE2AE67);      // 焦点暖杏

        public static Color Hex(int rgb, float a = 1f) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, a);

        /// <summary>小窗口：宽度不足 1600 时用 1280×720 那一档。</summary>
        public static bool Small => Screen.width < 1600;

        public static float TopBar => Small ? 76 : 84;
        public static float Corridor => Small ? 76 : 88;
        public static float PlaqueMargin => Small ? 24 : 32;
        public static float PlaqueWidth => Small ? 270 : 292;
        public static float RackMargin => Small ? 18 : 24;
        public static float RackWidth => Small ? 178 : 198;
        public static float ButtonHeight => 44;

        public static int Brand => Small ? 26 : 30;
        public static int StationName => Small ? 29 : 36;
        public static int PlaqueTitle => Small ? 21 : 24;
        public static int Body => Small ? 14 : 15;
        public static int Resource => 18;
        public static int Note => Small ? 11 : 12;

        /// <summary>场景推近约 1.1 秒；减少动效后取消推近过渡。</summary>
        public const float PushInSeconds = 1.1f;

        static Font song, sans;

        /// <summary>标题优先系统宋体，正文系统中文无衬线，不下载字体。</summary>
        // 用 Unity 的 == null：退出 Play 或换场景后旧字体对象已销毁，C# 的 ?? 看不出来
        public static Font Song => song != null ? song : (song = Font.CreateDynamicFontFromOSFont(new[] { "Songti SC", "STSong", "SimSun", "Noto Serif CJK SC", "PingFang SC" }, 32));
        public static Font Sans => sans != null ? sans : (sans = Font.CreateDynamicFontFromOSFont(new[] { "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", "Noto Sans CJK SC", "Arial Unicode MS" }, 32));
    }
}
