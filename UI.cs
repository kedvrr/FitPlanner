using System.Drawing;
using System.Windows.Forms;

namespace FitPlannerApp
{
    /// <summary>
    /// Modern light-theme palette. Inspired by Fluent / Material Design:
    /// warm off-white backgrounds, clean white cards, refined borders, vivid accents.
    /// </summary>
    public static class UI
    {
        // ── Backgrounds ───────────────────────────────────────────────────────
        public static readonly Color BgPage   = Color.FromArgb(245, 246, 250);   // warm off-white
        public static readonly Color BgSide   = Color.FromArgb(15,  20,  40);    // deep navy sidebar
        public static readonly Color BgCard   = Color.FromArgb(255, 255, 255);   // pure white cards
        public static readonly Color BgHeader = Color.FromArgb(248, 249, 255);   // pale blue-white headers
        public static readonly Color BgDialog = Color.FromArgb(252, 252, 255);   // dialog forms
        public static readonly Color BgInput  = Color.FromArgb(245, 247, 252);   // input fields
        public static readonly Color BgCancel = Color.FromArgb(235, 237, 248);   // cancel buttons
        public static readonly Color TileHover= Color.FromArgb(238, 242, 255);   // stat tile hover

        // ── Text ──────────────────────────────────────────────────────────────
        public static readonly Color TextDark  = Color.FromArgb(22,  24,  38);   // primary
        public static readonly Color TextMid   = Color.FromArgb(90,  96, 125);   // secondary
        public static readonly Color TextFaded = Color.FromArgb(155, 160, 185);  // hint

        // ── Accents ───────────────────────────────────────────────────────────
        public static readonly Color Cyan   = Color.FromArgb(0,   182, 200);
        public static readonly Color Red    = Color.FromArgb(232,  55,  70);
        public static readonly Color Orange = Color.FromArgb(252,  96,  55);
        public static readonly Color Green  = Color.FromArgb(34,  180, 100);
        public static readonly Color Gold   = Color.FromArgb(244, 180,  30);
        public static readonly Color Blue   = Color.FromArgb(60,  115, 230);
        public static readonly Color Purple = Color.FromArgb(148,  70, 220);

        // ── Chrome ────────────────────────────────────────────────────────────
        public static readonly Color Border = Color.FromArgb(228, 230, 240);

        // ═════════════════════════════════════════════════════════════════════
        //  FACTORIES (signatures identical to original — no panel code changes needed)
        // ═════════════════════════════════════════════════════════════════════

        public static Button MakeBtn(string text, Color bg, Color fg,
                                     int w = 140, int h = 36, float fontSize = 9.5f)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", fontSize, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(w, h),
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize        = 0;
            btn.FlatAppearance.MouseOverBackColor = LightenDarken(bg, 18);
            return btn;
        }

        public static Button MakeIconBtn(string icon, Color bg, int size = 32)
        {
            var btn = new Button
            {
                Text      = icon,
                Font      = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(size, size),
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        public static Label MakeLabel(string text, float size, FontStyle style,
                                      Color color, bool autoSize = true)
        {
            return new Label
            {
                Text      = text,
                Font      = new Font("Segoe UI", size, style),
                ForeColor = color,
                AutoSize  = autoSize,
                BackColor = Color.Transparent
            };
        }

        public static Panel MakeCard(int margin = 6)
        {
            return new Panel
            {
                BackColor   = BgCard,
                Margin      = new Padding(margin),
                Padding     = new Padding(0),
                BorderStyle = BorderStyle.None
            };
        }

        public static Panel MakeSep(bool horizontal = true, int thickness = 1)
        {
            return new Panel
            {
                BackColor = Border,
                Size      = horizontal ? new Size(1, thickness) : new Size(thickness, 1),
                Dock      = horizontal ? DockStyle.Top : DockStyle.Left
            };
        }

        public static Color LightenDarken(Color c, int amount)
        {
            return Color.FromArgb(
                Math.Clamp(c.R + amount, 0, 255),
                Math.Clamp(c.G + amount, 0, 255),
                Math.Clamp(c.B + amount, 0, 255));
        }
    }
}
