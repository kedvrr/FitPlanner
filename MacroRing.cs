using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FitPlannerApp
{
    public class MacroRing : Panel
    {
        // ── Public data ──────────────────────────────────────────────────────
        public string Label     { get; }
        public string Unit      { get; }
        public Color  RingColor { get; }

        // Current values (set via Update())
        double _consumed;
        double _planned;
        double _goal;

        // ── Animation state ───────────────────────────────────────────────────
        double _animConsumed;   // animated 0 → _consumed
        double _animPlanned;    // animated 0 → _planned
        readonly System.Windows.Forms.Timer _timer =
        new System.Windows.Forms.Timer { Interval = 16 };  // ~60 fps

        // ── Hover state ───────────────────────────────────────────────────────
        bool   _hovered;
        float  _hoverScale = 1f;
        System.Windows.Forms.Timer _hoverTimer =
        new System.Windows.Forms.Timer { Interval = 16 };

        // ── Constructor ───────────────────────────────────────────────────────
        public MacroRing(string label, Color color, string unit)
        {
            Label     = label;
            RingColor = color;
            Unit      = unit;

            Width      = 160;
            Height     = 160;
            BackColor  = UI.BgCard;
            DoubleBuffered = true;

            Paint += OnPaint;

            // Spin animation timer
            _timer.Tick += (s, e) =>
            {
                bool done = true;
                double speed = 0.08;   // how fast the ring fills (0–1 per frame)

                if (Math.Abs(_animConsumed - _consumed) > 0.5)
                { _animConsumed += (_consumed - _animConsumed) * speed; done = false; }
                else _animConsumed = _consumed;

                if (Math.Abs(_animPlanned - _planned) > 0.5)
                { _animPlanned += (_planned - _planned == 0 ? 0 : (_planned - _animPlanned) * speed); done = false; }
                else _animPlanned = _planned;

                if (done) _timer.Stop();
                Invalidate();
            };

            // Hover pulse timer
            _hoverTimer.Tick += (s, e) =>
            {
                float target = _hovered ? 1.06f : 1.0f;
                _hoverScale += (target - _hoverScale) * 0.18f;
                if (Math.Abs(_hoverScale - target) < 0.001f)
                { _hoverScale = target; _hoverTimer.Stop(); }
                Invalidate();
            };

            MouseEnter += (s, e) => { _hovered = true;  _hoverTimer.Start(); };
            MouseLeave += (s, e) => { _hovered = false; _hoverTimer.Start(); };
        }

        // ── Update values + start animation ──────────────────────────────────
        public void Update(double consumed, double planned, double goal)
        {
            _consumed = consumed;
            _planned  = planned;
            _goal     = goal > 0 ? goal : 1;

            // Reset animation from zero for dramatic effect
            _animConsumed = 0;
            _animPlanned  = 0;

            _timer.Start();
        }

        // ── Paint ─────────────────────────────────────────────────────────────
        void OnPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode      = SmoothingMode.AntiAlias;
            g.TextRenderingHint  = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            int  cx = Width  / 2;
            int  cy = Height / 2;

            // Scale slightly on hover
            float  s      = _hoverScale;
            float  outer  = Math.Min(Width, Height) / 2f * 0.88f * s;
            float  thick  = outer * 0.28f;
            float  inner  = outer - thick;
            RectangleF arc = new RectangleF(cx - outer, cy - outer, outer * 2, outer * 2);

            // ── 1. Track ring (background) ────────────────────────────────────
            Color trackColor = Color.FromArgb(22, RingColor.R, RingColor.G, RingColor.B);
            using (var pen = new Pen(trackColor, thick) { LineJoin = LineJoin.Round })
                g.DrawEllipse(pen, arc);

            // ── 2. Planned arc (softer, thinner) ─────────────────────────────
            float plannedPct = (float)Math.Min(1.0, _animPlanned / _goal);
            if (plannedPct > 0.001f)
            {
                float sweep = plannedPct * 360f;
                Color planColor = Color.FromArgb(80, RingColor.R, RingColor.G, RingColor.B);
                using var planPen = new Pen(planColor, thick - 2f)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(planPen, arc, -90f, sweep);
            }

            // ── 3. Consumed arc (full colour, rounded caps) ───────────────────
            float consumedPct = (float)Math.Min(1.0, _animConsumed / _goal);
            if (consumedPct > 0.001f)
            {
                float sweep = consumedPct * 360f;
                using var conPen = new Pen(RingColor, thick)
                    { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawArc(conPen, arc, -90f, sweep);
            }

            // ── 4. Centre text ────────────────────────────────────────────────
            // Big number (consumed)
            string bigText  = _consumed > 0 ? ((int)_animConsumed).ToString() : "0";
            var    bigFont  = new Font("Segoe UI", outer * 0.28f, FontStyle.Bold);
            var    bigSize  = g.MeasureString(bigText, bigFont);
            using (var b = new SolidBrush(UI.TextDark))
                g.DrawString(bigText, bigFont, b,
                    cx - bigSize.Width / 2f,
                    cy - bigSize.Height * 0.60f);

            // Unit (small, below number)
            var unitFont = new Font("Segoe UI", outer * 0.14f, FontStyle.Regular);
            var unitSize = g.MeasureString(Unit, unitFont);
            using (var b = new SolidBrush(UI.TextFaded))
                g.DrawString(Unit, unitFont, b,
                    cx - unitSize.Width / 2f,
                    cy + bigSize.Height * 0.05f);

            // ── 5. Label below ring ────────────────────────────────────────────
            var lblFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            var lblSize = g.MeasureString(Label, lblFont);
            using (var b = new SolidBrush(_hovered ? RingColor : UI.TextMid))
                g.DrawString(Label, lblFont, b,
                    cx - lblSize.Width / 2f,
                    cy + outer + 6f);

            // ── 6. Percentage pill below label ────────────────────────────────
            if (_goal > 0 && _consumed > 0)
            {
                int pct      = (int)Math.Min(100, _animConsumed / _goal * 100);
                string pctTxt = $"{pct}%";
                var pctFont  = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                var pctSize  = g.MeasureString(pctTxt, pctFont);
                float px = cx - pctSize.Width / 2f - 4;
                float py = cy + outer + 20f;
                var pillRect = new RectangleF(px, py, pctSize.Width + 8, pctSize.Height + 2);
                Color pillBg = Color.FromArgb(30, RingColor.R, RingColor.G, RingColor.B);
                using (var b = new SolidBrush(pillBg))
                    g.FillRectangle(b, pillRect);
                using (var b = new SolidBrush(RingColor))
                    g.DrawString(pctTxt, pctFont, b, px + 4, py + 1);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer.Dispose(); _hoverTimer.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
