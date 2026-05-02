// ─────────────────────────────────────────────────────────────────────────────
// MainForm.cs — Sidebar rewrite only.
// Content area, panels, and all other files are completely unchanged.
//
// NEW SIDEBAR FEATURES:
//   Feature 1 — Emoji icons next to every nav item (drawn via Graphics)
//   Feature 2 — Smooth hover highlight (MouseEnter / MouseLeave on NavItem)
//   Feature 3 — 3 px vertical cyan accent bar on the active nav item
//   Feature 4 — Collapse / Expand toggle button (240 px ↔ 70 px)
//   Feature 5 — Improved user profile section with circular avatar + details
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FitPlannerApp
{
    public class MainForm : Form
    {
        // ── Sidebar state ──────────────────────────────────────────────────
        Panel sideBar, contentArea;
        bool  sidebarCollapsed = false;

        const int SW_EXPANDED  = 240;
        const int SW_COLLAPSED = 70;

        // ── Sidebar sections ───────────────────────────────────────────────
        Panel  logoSection;
        Button btnToggle;
        Label  navSectionLabel;
        Panel  profileSection;

        // ── Nav items (custom painted panels) ─────────────────────────────
        NavItem navDash, navWorkout, navDiet;

        // ── Content panels (unchanged) ─────────────────────────────────────
        DashboardPanel      dashPanel;
        WorkoutPlannerPanel workoutPanel;
        DietPlannerPanel    dietPanel;

        WorkoutPlan plan;
        DietPlan    diet;

        // ─────────────────────────────────────────────────────────────────────
        public MainForm()
        {
            plan = SeedData.DefaultWorkout();
            diet = SeedData.DefaultDiet();
            SeedData.InitAppState(plan, diet);
            Build();
            ShowPage(dashPanel, navDash);   // start on dashboard
        }

        // ─────────────────────────────────────────────────────────────────────
        void Build()
        {
            Text          = "FitPlanner — Workout & Diet Manager";
            Size          = new Size(1340, 840);
            MinimumSize   = new Size(1100, 720);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor     = UI.BgSide;

            // ═══════════════════════════════════════════════════════════════
            //  SIDEBAR
            // ═══════════════════════════════════════════════════════════════
            sideBar = new Panel
            {
                Dock      = DockStyle.Left,
                Width     = SW_EXPANDED,
                BackColor = UI.BgSide
            };

            // ─── Logo section (Feature 4: repaints on collapse) ──────────
            logoSection = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 94,
                BackColor = Color.FromArgb(10, 14, 32)
            };
            logoSection.Paint += DrawLogo;

            // ─── Toggle button (Feature 4) ────────────────────────────────
            btnToggle = new Button
            {
                Bounds    = new Rectangle(0, 94, SW_EXPANDED, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(22, 255, 255, 255),
                ForeColor = Color.FromArgb(130, 135, 165),
                Font      = new Font("Segoe UI", 8f, FontStyle.Bold),
                Text      = "◀  Collapse",
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnToggle.FlatAppearance.BorderSize         = 0;
            btnToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(32, 255, 255, 255);
            btnToggle.Click += OnToggleSidebar;

            // ─── "NAVIGATION" label ───────────────────────────────────────
            navSectionLabel = new Label
            {
                Text      = "NAVIGATION",
                Font      = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(85, 92, 130),
                AutoSize  = false,
                Bounds    = new Rectangle(0, 132, SW_EXPANDED, 24),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding   = new Padding(22, 0, 0, 0),
                BackColor = Color.Transparent
            };

            // ─── Nav items (Features 1, 2, 3) ─────────────────────────────
            // Y positions: after logo(94) + toggle(34) + navLabel(24) = 152
            navDash    = new NavItem("📊", "Dashboard",       160);
            navWorkout = new NavItem("🏋", "Workout Planner", 222);
            navDiet    = new NavItem("🥗", "Diet Planner",    284);

            // Wire widths so they span the sidebar
            navDash.Width    = SW_EXPANDED;
            navWorkout.Width = SW_EXPANDED;
            navDiet.Width    = SW_EXPANDED;

            navDash.Clicked    += () => ShowPage(dashPanel,    navDash);
            navWorkout.Clicked += () => ShowPage(workoutPanel, navWorkout);
            navDiet.Clicked    += () => ShowPage(dietPanel,    navDiet);

            // ─── User profile section (Feature 5) ─────────────────────────
            profileSection = BuildProfileSection();

            // Assemble sidebar
            sideBar.Controls.Add(logoSection);
            sideBar.Controls.Add(btnToggle);
            sideBar.Controls.Add(navSectionLabel);
            sideBar.Controls.Add(navDash);
            sideBar.Controls.Add(navWorkout);
            sideBar.Controls.Add(navDiet);
            sideBar.Controls.Add(profileSection);   // DockStyle.Bottom — added last

            // ═══════════════════════════════════════════════════════════════
            //  CONTENT AREA (completely unchanged)
            // ═══════════════════════════════════════════════════════════════
            contentArea = new Panel { Dock = DockStyle.Fill, BackColor = UI.BgPage };

            dashPanel    = new DashboardPanel(plan, diet)  { Dock = DockStyle.Fill, Visible = false };
            workoutPanel = new WorkoutPlannerPanel(plan)    { Dock = DockStyle.Fill, Visible = false };
            dietPanel    = new DietPlannerPanel(diet)       { Dock = DockStyle.Fill, Visible = false };

            contentArea.Controls.Add(dietPanel);
            contentArea.Controls.Add(workoutPanel);
            contentArea.Controls.Add(dashPanel);

            Controls.Add(contentArea);
            Controls.Add(sideBar);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Logo painter — adapts to collapsed / expanded state (Feature 4)
        // ─────────────────────────────────────────────────────────────────────
        void DrawLogo(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (!sidebarCollapsed)
            {
                // Expanded: circle + "FP" + app name + subtitle
                using (var b = new SolidBrush(UI.Cyan))
                    g.FillEllipse(b, 16, 18, 52, 52);
                using (var b = new SolidBrush(Color.White))
                {
                    var f = new Font("Segoe UI", 14, FontStyle.Bold);
                    var sz = g.MeasureString("FP", f);
                    g.DrawString("FP", f, b, 16 + (52 - sz.Width) / 2, 18 + (52 - sz.Height) / 2);
                }
                using (var b = new SolidBrush(Color.White))
                    g.DrawString("FitPlanner", new Font("Segoe UI", 15, FontStyle.Bold), b, 78, 19);
                using (var b = new SolidBrush(UI.TextFaded))
                    g.DrawString("Health & Fitness", new Font("Segoe UI", 8f), b, 80, 50);
            }
            else
            {
                // Collapsed: just the circle, centred in 70 px
                int cx = (SW_COLLAPSED - 48) / 2;
                using (var b = new SolidBrush(UI.Cyan))
                    g.FillEllipse(b, cx, 18, 48, 48);
                using (var b = new SolidBrush(Color.White))
                {
                    var f  = new Font("Segoe UI", 12, FontStyle.Bold);
                    var sz = g.MeasureString("FP", f);
                    g.DrawString("FP", f, b, cx + (48 - sz.Width) / 2, 18 + (48 - sz.Height) / 2);
                }
            }

            // Horizontal divider at bottom of logo section
            using (var p = new Pen(Color.FromArgb(45, 255, 255, 255)))
                g.DrawLine(p, 0, 92, sideBar.Width, 92);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Feature 5 — User profile section
        // ─────────────────────────────────────────────────────────────────────
        Panel BuildProfileSection()
        {
            var panel = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 74,
                BackColor = Color.FromArgb(10, 14, 32)
            };

            panel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Top separator
                using (var p = new Pen(Color.FromArgb(40, 255, 255, 255)))
                    g.DrawLine(p, 0, 0, sideBar.Width, 0);

                const int AvatarSize = 42;
                int avatarX = sidebarCollapsed
                    ? (SW_COLLAPSED - AvatarSize) / 2   // centred when collapsed
                    : 14;
                int avatarY = (panel.Height - AvatarSize) / 2;

                // Circular avatar — draw as filled ellipse
                using (var b = new SolidBrush(UI.Cyan))
                    g.FillEllipse(b, avatarX, avatarY, AvatarSize, AvatarSize);

                // Avatar ring
                using (var p = new Pen(Color.FromArgb(60, 255, 255, 255), 2))
                    g.DrawEllipse(p, avatarX, avatarY, AvatarSize, AvatarSize);

                // Initials inside avatar
                string initials = sidebarCollapsed ? "U" : "FU";
                var initFont    = new Font("Segoe UI", sidebarCollapsed ? 13f : 11f, FontStyle.Bold);
                var initSz      = g.MeasureString(initials, initFont);
                using (var b = new SolidBrush(Color.White))
                    g.DrawString(initials, initFont, b,
                        avatarX + (AvatarSize - initSz.Width)  / 2,
                        avatarY + (AvatarSize - initSz.Height) / 2);

                // Text only when expanded
                if (!sidebarCollapsed)
                {
                    int tx = avatarX + AvatarSize + 10;
                    using (var b = new SolidBrush(Color.White))
                        g.DrawString("FitPlanner User",
                            new Font("Segoe UI", 9f, FontStyle.Bold), b, tx, 17);
                    using (var b = new SolidBrush(UI.TextFaded))
                        g.DrawString(".NET Project 2025",
                            new Font("Segoe UI", 7.5f), b, tx, 38);

                    // Small "online" green dot
                    using (var b = new SolidBrush(Color.FromArgb(80, 200, 120)))
                        g.FillEllipse(b, avatarX + AvatarSize - 11, avatarY + AvatarSize - 11, 12, 12);
                    using (var p = new Pen(Color.FromArgb(14, 11, 22), 2))
                        g.DrawEllipse(p, avatarX + AvatarSize - 11, avatarY + AvatarSize - 11, 12, 12);
                }
            };

            return panel;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Feature 4 — Collapse / expand handler
        // ─────────────────────────────────────────────────────────────────────
        void OnToggleSidebar(object? sender, EventArgs e)
        {
            sidebarCollapsed = !sidebarCollapsed;
            int w = sidebarCollapsed ? SW_COLLAPSED : SW_EXPANDED;

            sideBar.Width  = w;
            btnToggle.Width = w;
            btnToggle.Text  = sidebarCollapsed ? "▶" : "◀  Collapse";

            // Resize nav items
            navDash.Resize(w, sidebarCollapsed);
            navWorkout.Resize(w, sidebarCollapsed);
            navDiet.Resize(w, sidebarCollapsed);

            // Hide/show the "NAVIGATION" section label
            navSectionLabel.Width   = w;
            navSectionLabel.Visible = !sidebarCollapsed;

            // Repaint logo + profile
            logoSection.Invalidate();
            profileSection.Invalidate();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  ShowPage — switch content + update active nav item
        // ─────────────────────────────────────────────────────────────────────
        void ShowPage(Panel target, NavItem active)
        {
            dashPanel.Visible    = false;
            workoutPanel.Visible = false;
            dietPanel.Visible    = false;
            target.Visible       = true;

            navDash.SetActive(active == navDash);
            navWorkout.SetActive(active == navWorkout);
            navDiet.SetActive(active == navDiet);
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  NavItem — custom sidebar navigation panel
    //
    //  Features implemented:
    //    1. Icon (emoji) + label text, drawn with Graphics
    //    2. Hover highlight (MouseEnter/Leave)
    //    3. Active 3 px cyan accent bar on left edge
    //    4. Collapses to icon-only when sidebar shrinks to 70 px
    // ═════════════════════════════════════════════════════════════════════════
    public class NavItem : Panel
    {
        // ── Palette ──────────────────────────────────────────────────────
        static readonly Color C_TEXT_NORMAL  = Color.FromArgb(175, 178, 200);
        static readonly Color C_TEXT_ACTIVE  = Color.FromArgb(0,   196, 214);
        static readonly Color C_BG_HOVER     = Color.FromArgb(22,  255, 255, 255);
        static readonly Color C_BG_ACTIVE    = Color.FromArgb(50,  0,   196, 214);
        static readonly Color C_ACCENT_BAR   = Color.FromArgb(0,   196, 214);
        static readonly Color C_ICON_NORMAL  = Color.FromArgb(130, 135, 165);

        readonly string _icon;
        readonly string _label;

        bool _isActive;
        bool _isHovered;
        bool _isCollapsed;

        // Public event so MainForm can subscribe
        public event Action? Clicked;

        public NavItem(string icon, string label, int top)
        {
            _icon  = icon;
            _label = label;

            // Absolute position in sidebar
            Location  = new Point(0, top);
            Height    = 60;
            Width     = 240;   // overridden after construction
            BackColor = Color.Transparent;
            Cursor    = Cursors.Hand;

            Paint      += OnPaint;
            MouseEnter += (s, e) => { _isHovered = true;  Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            MouseClick += (s, e) => Clicked?.Invoke();
        }

        public void SetActive(bool active)
        {
            _isActive = active;
            Invalidate();
        }

        // Called by MainForm when sidebar is toggled (Feature 4)
        public void Resize(int sidebarWidth, bool collapsed)
        {
            _isCollapsed = collapsed;
            Width = sidebarWidth;
            Invalidate();
        }

        void OnPaint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // ── Background ────────────────────────────────────────────────
            Color bg = _isActive  ? C_BG_ACTIVE
                     : _isHovered ? C_BG_HOVER
                     :              Color.Transparent;
            if (bg != Color.Transparent)
                g.FillRectangle(new SolidBrush(bg), 0, 0, Width, Height);

            // ── Feature 3 — Active accent bar (3 px, vertically centred) ──
            if (_isActive)
            {
                int barH = 34, barY = (Height - barH) / 2;
                using (var b = new SolidBrush(C_ACCENT_BAR))
                    g.FillRectangle(b, 0, barY, 3, barH);
            }

            // ── Feature 1 — Icon ─────────────────────────────────────────
            Color iconColor = _isActive ? C_TEXT_ACTIVE : C_ICON_NORMAL;
            Color txtColor  = _isActive ? C_TEXT_ACTIVE : C_TEXT_NORMAL;

            if (_isCollapsed)
            {
                // Collapsed — draw icon centred horizontally in the 70 px panel
                var iconFont = new Font("Segoe UI Emoji", 20);
                var iconSz   = g.MeasureString(_icon, iconFont);
                float ix     = (Width  - iconSz.Width)  / 2f;
                float iy     = (Height - iconSz.Height) / 2f;
                using (var b = new SolidBrush(iconColor))
                    g.DrawString(_icon, iconFont, b, ix, iy);
            }
            else
            {
                // Expanded — icon on left, label beside it

                // Icon (slightly larger emoji)
                var iconFont = new Font("Segoe UI Emoji", 16);
                var iconSz   = g.MeasureString(_icon, iconFont);
                float iy     = (Height - iconSz.Height) / 2f - 1f;
                using (var b = new SolidBrush(iconColor))
                    g.DrawString(_icon, iconFont, b, 20, iy);

                // Label
                var lblFont = new Font("Segoe UI", 10.5f,
                    _isActive ? FontStyle.Bold : FontStyle.Regular);
                var lblSz   = g.MeasureString(_label, lblFont);
                float ly    = (Height - lblSz.Height) / 2f;
                using (var b = new SolidBrush(txtColor))
                    g.DrawString(_label, lblFont, b, 58, ly);

                // Feature 2 — Hover: subtle right arrow hint
                if (_isHovered && !_isActive)
                {
                    using (var b = new SolidBrush(Color.FromArgb(80, 175, 178, 200)))
                        g.DrawString("›", new Font("Segoe UI", 14, FontStyle.Bold), b,
                            Width - 22, (Height - 18) / 2f);
                }
            }

            // Bottom separator between items (subtle)
            using (var p = new Pen(Color.FromArgb(18, 255, 255, 255)))
                g.DrawLine(p, 8, Height - 1, Width - 8, Height - 1);
        }
    }
}
