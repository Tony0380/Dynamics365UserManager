using System.Drawing;

namespace Dynamics365UserManager
{
    public static class AppTheme
    {
        public static bool IsDark { get; set; } = false;

        // ── Backgrounds ──
        public static Color FormBg        => IsDark ? Color.FromArgb(24, 24, 28)   : Color.FromArgb(243, 243, 243);
        public static Color ControlBg     => IsDark ? Color.FromArgb(32, 32, 36)   : Color.White;
        public static Color InputBg       => IsDark ? Color.FromArgb(42, 42, 48)   : Color.White;
        public static Color ListBg        => IsDark ? Color.FromArgb(28, 28, 32)   : Color.White;
        public static Color LogBg         => IsDark ? Color.FromArgb(18, 18, 22)   : Color.FromArgb(250, 250, 250);
        public static Color TabBg         => IsDark ? Color.FromArgb(28, 28, 32)   : Color.FromArgb(235, 235, 240);
        public static Color TabActiveBg   => IsDark ? Color.FromArgb(42, 42, 48)   : Color.White;
        public static Color BtnBg         => IsDark ? Color.FromArgb(38, 38, 44)   : Color.FromArgb(232, 232, 236);

        // ── Foregrounds ──
        public static Color FgPrimary     => IsDark ? Color.FromArgb(230, 230, 235) : Color.FromArgb(28, 28, 32);
        public static Color FgSecondary   => IsDark ? Color.FromArgb(160, 160, 170) : Color.FromArgb(90, 90, 100);
        public static Color FgPlaceholder => IsDark ? Color.FromArgb(110, 110, 120) : Color.FromArgb(160, 160, 170);
        public static Color LogFg         => IsDark ? Color.FromArgb(190, 190, 200) : Color.FromArgb(50, 50, 60);

        // ── Accent / Blue ──
        public static Color AccentBlue    => IsDark ? Color.FromArgb(56, 132, 244)  : Color.FromArgb(0, 102, 204);
        public static Color AccentHover   => IsDark ? Color.FromArgb(80, 152, 255)  : Color.FromArgb(20, 120, 220);
        public static Color BtnHover      => IsDark ? Color.FromArgb(50, 50, 58)   : Color.FromArgb(218, 218, 224);

        // ── Borders ──
        public static Color Border        => IsDark ? Color.FromArgb(55, 55, 62)   : Color.FromArgb(200, 200, 208);
        public static Color InputBorder   => IsDark ? Color.FromArgb(65, 65, 72)   : Color.FromArgb(180, 180, 190);

        // ── Semantic ──
        public static Color Success       => Color.FromArgb(60, 180, 100);
        public static Color Error         => Color.FromArgb(230, 72, 72);
        public static Color Warning       => Color.FromArgb(230, 160, 40);
        public static Color InfoBlue      => IsDark ? Color.FromArgb(100, 170, 255) : Color.FromArgb(0, 90, 190);
        public static Color LogTimestamp  => IsDark ? Color.FromArgb(80, 140, 220)  : Color.FromArgb(40, 100, 180);

        // ── Link ──
        public static Color Link          => IsDark ? Color.FromArgb(100, 170, 255) : Color.FromArgb(0, 90, 190);
        public static Color LinkActive    => IsDark ? Color.White                   : Color.FromArgb(0, 60, 150);

        // ── CheckedListBox ──
        public static Color CheckListBg   => IsDark ? Color.FromArgb(34, 34, 40)   : Color.White;
        public static Color CheckListFg   => IsDark ? Color.FromArgb(220, 220, 225) : Color.FromArgb(28, 28, 32);
    }
}
