using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace FitPlannerApp
{
    public class DashboardPanel : Panel
    {
        WorkoutPlan wp;
        DietPlan    dp;

        // ── Feature 5 — Live summary tile labels ──────────────────────────
        Label       lblSumEx, lblSumCal, lblSumRemain, lblSumPro;
        ProgressBar barSumEx, barSumCal;

        // ── Feature 1 — Quick action dialog fields ────────────────────────
        // (stored so quick-log dialogs can pre-fill data)
        TextBox? qaWeightBox;    // used in Update Weight quick dialog

        // ── Feature 3 — Streak display label ─────────────────────────────
        Label lblStreakNum;

        // ── Feature 4 — Goal progress labels ─────────────────────────────
        Label       lblGoalCurrent, lblGoalTarget, lblGoalDiff;
        ProgressBar barGoal;

        // ─────────────────────────────────────────────────────────────────
        public DashboardPanel(WorkoutPlan workout, DietPlan diet)
        {
            wp = workout; dp = diet;
            BackColor  = UI.BgPage;
            AutoScroll = true;
            Build();
            AppState.StatsChanged += () =>
            {
                // REQUIREMENT 5: When food is checked in DietPlannerPanel,
                // AppState.UpdateNutrition() fires StatsChanged, which lands here
                // and triggers RefreshDashboard() on the UI thread.
                if (InvokeRequired) Invoke(new Action(RefreshDashboard));
                else                RefreshDashboard();
            };
        }

        // ═════════════════════════════════════════════════════════════════
        //  BUILD — section order matches the spec
        // ═════════════════════════════════════════════════════════════════
        void Build()
        {
            // ── Page header ───────────────────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 76, BackColor = UI.BgPage };
            var t1  = UI.MakeLabel("Dashboard", 22, FontStyle.Bold, UI.TextDark);
            t1.Location = new Point(26, 16);
            var t2  = UI.MakeLabel($"Overview  —  {DateTime.Today:dddd, MMMM d yyyy}",
                                   9.5f, FontStyle.Regular, UI.TextMid);
            t2.Location = new Point(26, 50);
            hdr.Controls.AddRange(new Control[] { t1, t2 });
            Controls.Add(hdr);

            // ══ ROW 1 — Feature 5: Improved live stat cards ══════════════
            Controls.Add(BuildLiveStatRow());

            // ══ ROW 2 — Feature 1: Quick Action Buttons ══════════════════
            Controls.Add(BuildQuickActions());

            // ══ ROW 3 — Feature 3 & 4: Streak + Goal cards side-by-side ═
            Controls.Add(BuildStreakAndGoalRow());

            // ══ ROW 4 — Feature 2: Weekly Progress Chart ═════════════════
            Controls.Add(BuildWeeklyChart());

            // ══ ROW 5 — Feature 6: Workout plan + Meal summary ═══════════
            Controls.Add(BuildBodyRow());

            // ══ ROW 6 — BMI Calculator (existing, unchanged) ═════════════
            Controls.Add(BuildBmiCard());

            // ══ ROW 7 — Progress Tracker (existing, unchanged) ═══════════
            Controls.Add(BuildProgressCard());
        }

        // ═════════════════════════════════════════════════════════════════
        //  FEATURE 5 — Live Stat Cards  (larger numbers, progress bars)
        // ═════════════════════════════════════════════════════════════════
        Panel BuildLiveStatRow()
        {
            var row = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 142,
                BackColor = UI.BgPage
            };
            var lbl = UI.MakeLabel("Today at a Glance", 11, FontStyle.Bold, UI.TextDark);
            lbl.Location = new Point(20, 8);
            row.Controls.Add(lbl);

            var flow = new FlowLayoutPanel
            {
                BackColor     = UI.BgPage,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Bounds        = new Rectangle(16, 30, row.Width - 32, 104),
                Anchor        = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            row.SizeChanged += (s, e) => flow.Width = row.Width - 32;

            // Four live stat tiles — bigger value font (22pt)
            flow.Controls.Add(BigStatTile("Exercises Done",    UI.Cyan,   true,  out lblSumEx,     out barSumEx));
            flow.Controls.Add(BigStatTile("Calories Consumed", UI.Orange, true,  out lblSumCal,    out barSumCal));
            flow.Controls.Add(BigStatTile("Calories Remaining",UI.Green,  false, out lblSumRemain, out _));
            flow.Controls.Add(BigStatTile("Protein Consumed",  UI.Blue,   false, out lblSumPro,    out _));

            row.Controls.Add(flow);
            RefreshDashboard();   // initial population on load
            return row;
        }

        Panel BigStatTile(string heading, Color accent, bool hasBar,
                          out Label valLbl, out ProgressBar pbar)
        {
            var tile = new Panel
            {
                Width     = 208,
                Height    = 100,
                BackColor = UI.BgCard,
                Margin    = new Padding(0, 0, 14, 0)
            };

            // Left accent bar
            tile.Controls.Add(new Panel { BackColor = accent, Width = 6, Dock = DockStyle.Left });

            var lHead = UI.MakeLabel(heading, 8.5f, FontStyle.Regular, UI.TextMid);
            lHead.Location = new Point(15, 10);

            // Large value — 22pt bold
            valLbl = UI.MakeLabel("—", 22, FontStyle.Bold, accent, false);
            valLbl.Location = new Point(13, 26);
            valLbl.Size     = new Size(188, 38);

            pbar = new ProgressBar
            {
                Bounds  = new Rectangle(13, 74, 186, 10),
                Minimum = 0, Maximum = 100,
                Style   = ProgressBarStyle.Continuous,
                ForeColor = accent,
                Visible = hasBar
            };

            tile.Controls.AddRange(new Control[] { lHead, valLbl, pbar });

            // Hover animation — smooth background fade using a Timer
            var hoverTimer = new System.Windows.Forms.Timer { Interval = 16 };
            bool tileHovered = false;
            float hoverBlend = 0f;  // 0 = normal, 1 = hovered

            hoverTimer.Tick += (ts, te) =>
            {
                float target = tileHovered ? 1f : 0f;
                hoverBlend += (target - hoverBlend) * 0.20f;
                if (Math.Abs(hoverBlend - target) < 0.02f) { hoverBlend = target; hoverTimer.Stop(); }
                // Interpolate BgCard → TileHover
                int r = (int)(UI.BgCard.R + (UI.TileHover.R - UI.BgCard.R) * hoverBlend);
                int g2 = (int)(UI.BgCard.G + (UI.TileHover.G - UI.BgCard.G) * hoverBlend);
                int b2 = (int)(UI.BgCard.B + (UI.TileHover.B - UI.BgCard.B) * hoverBlend);
                tile.BackColor = Color.FromArgb(r, g2, b2);
            };
            tile.MouseEnter += (s, e) => { tileHovered = true;  hoverTimer.Start(); };
            tile.MouseLeave += (s, e) => { tileHovered = false; hoverTimer.Start(); };

            return tile;
        }

        // =====================================================================
        //  REQUIREMENT 4 — RefreshDashboard()
        //  Reads from AppState explicit properties (set by DietPlannerPanel).
        //  Updates: Calories Consumed, Calories Remaining, Protein Consumed,
        //           Exercises Done, progress bars.
        //  Called automatically via AppState.StatsChanged (Requirement 5).
        // =====================================================================
        public void RefreshDashboard()
        {
            // ── Workout progress ────────────────────────────────────────────
            int done  = AppState.TodayExercisesCompleted;
            int total = AppState.TotalExercises;
            lblSumEx.Text  = total > 0 ? $"{done} / {total}" : "—";
            barSumEx.Value = total > 0 ? Math.Min(100, (int)((double)done / total * 100)) : 0;

            // ── REQUIREMENT 3: Read from AppState explicit properties ────────
            // These are written by DietPlannerPanel.RefreshMacros() via
            // AppState.UpdateNutrition() — not computed on the fly from the model.
            double calGoal = AppState.DailyCalorieGoal;

            lblSumCal.Text   = $"{AppState.CaloriesConsumed:N0} kcal";
            barSumCal.Value  = calGoal > 0
                ? Math.Min(100, (int)(AppState.CaloriesConsumed / calGoal * 100))
                : 0;

            lblSumRemain.Text = $"{AppState.CaloriesRemaining:N0} kcal";
            lblSumPro.Text    = $"{AppState.ProteinConsumed:N0} g";

            // ── Keep streak and goal cards in sync ───────────────────────────
            RefreshStreak();
            RefreshGoal();
        }

        // ═════════════════════════════════════════════════════════════════
        //  FEATURE 1 — Quick Action Buttons
        // ═════════════════════════════════════════════════════════════════
        Panel BuildQuickActions()
        {
            var row = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = UI.BgPage
            };

            var lbl = UI.MakeLabel("Quick Actions", 11, FontStyle.Bold, UI.TextDark);
            lbl.Location = new Point(20, 8);
            row.Controls.Add(lbl);

            var flow = new FlowLayoutPanel
            {
                BackColor     = UI.BgPage,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = false,
                Bounds        = new Rectangle(16, 32, row.Width - 32, 32),
                Anchor        = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            row.SizeChanged += (s, e) => flow.Width = row.Width - 32;

            var btnLogWorkout  = QuickBtn("🏋  Log Workout",   UI.Cyan,   UI.TextDark);
            var btnAddMeal     = QuickBtn("🥗  Add Meal",       UI.Orange, Color.White);
            var btnUpdateWeight= QuickBtn("⚖  Update Weight",  UI.Green,  Color.White);

            btnLogWorkout.Click   += (s, e) => QuickLogWorkout();
            btnAddMeal.Click      += (s, e) => QuickAddMeal();
            btnUpdateWeight.Click += (s, e) => QuickUpdateWeight();

            flow.Controls.AddRange(new Control[] { btnLogWorkout, btnAddMeal, btnUpdateWeight });
            row.Controls.Add(flow);
            return row;
        }

        Button QuickBtn(string text, Color bg, Color fg)
        {
            var b = new Button
            {
                Text      = text,
                Font      = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = fg,
                BackColor = bg,
                FlatStyle = FlatStyle.Flat,
                Width     = 168,
                Height    = 34,
                Cursor    = Cursors.Hand,
                Margin    = new Padding(0, 0, 12, 0),
                UseVisualStyleBackColor = false
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(
                Math.Min(255, bg.R + 20),
                Math.Min(255, bg.G + 20),
                Math.Min(255, bg.B + 20));
            return b;
        }

        void QuickLogWorkout()
        {
            // Small inline dialog — mark exercises completed for today
            using var dlg = new Form
            {
                Text = "Log Workout", ClientSize = new Size(360, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, BackColor = UI.BgDialog
            };
            var lbl = new Label { Text = "Mark a workout day as completed:", AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid };
            var combo = new ComboBox { Bounds = new Rectangle(20, 44, 316, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            foreach (var day in wp.Days) combo.Items.Add(day);
            if (combo.Items.Count > 0) combo.SelectedIndex = 0;

            var btnDone = new Button { Text = "Mark All Exercises Done", Bounds = new Rectangle(20, 88, 220, 36), FlatStyle = FlatStyle.Flat, BackColor = UI.Cyan, ForeColor = UI.TextDark, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnDone.FlatAppearance.BorderSize = 0;
            btnDone.Click += (s, e) => {
                if (combo.SelectedItem is WorkoutDay wd)
                {
                    foreach (var ex in wd.Exercises) ex.IsCompleted = true;
                    AppState.NotifyStatsChanged();
                    MessageBox.Show($"All exercises in \"{wd.DayLabel}\" marked as done!", "Workout Logged", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                }
            };
            var btnCancel = new Button { Text = "Cancel", Bounds = new Rectangle(256, 88, 80, 36), FlatStyle = FlatStyle.Flat, BackColor = UI.BgCancel, ForeColor = UI.TextMid, Font = new Font("Segoe UI", 10), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            btnCancel.FlatAppearance.BorderSize = 0;

            var lStreak = new Label { Text = $"Current streak: {ComputeStreak()} day(s)", AutoSize = true, Location = new Point(20, 140), Font = new Font("Segoe UI", 9), ForeColor = UI.TextMid };
            dlg.Controls.AddRange(new Control[] { lbl, combo, btnDone, btnCancel, lStreak });
            dlg.AcceptButton = btnDone; dlg.CancelButton = btnCancel;
            dlg.ShowDialog(this);
        }

        void QuickAddMeal()
        {
            using var dlg = new Form
            {
                Text = "Quick Add Meal", ClientSize = new Size(380, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, BackColor = UI.BgDialog
            };
            var lDay = new Label { Text = "Day:", AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid };
            var cboDay = new ComboBox { Bounds = new Rectangle(20, 40, 168, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            foreach (var d in dp.Week) cboDay.Items.Add(d);
            if (cboDay.Items.Count > 0) cboDay.SelectedIndex = 0;

            var lType = new Label { Text = "Meal Type:", AutoSize = true, Location = new Point(204, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid };
            var cboType = new ComboBox { Bounds = new Rectangle(204, 40, 156, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            foreach (MealType mt in Enum.GetValues(typeof(MealType))) cboType.Items.Add(mt);
            cboType.SelectedIndex = 0;

            var lName = new Label { Text = "Custom Name (optional):", AutoSize = true, Location = new Point(20, 82), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid };
            var txtName = new TextBox { Bounds = new Rectangle(20, 104, 340, 26), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. Post-workout shake" };

            var btnAdd = new Button { Text = "Add Meal", Bounds = new Rectangle(236, 148, 124, 36), FlatStyle = FlatStyle.Flat, BackColor = UI.Orange, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAdd.FlatAppearance.BorderSize = 0;
            btnAdd.Click += (s, e) => {
                if (cboDay.SelectedItem is DayMealPlan day && cboType.SelectedItem is MealType mt)
                {
                    day.Meals.Add(new Meal { Type = mt, CustomName = txtName.Text.Trim() });
                    AppState.NotifyStatsChanged();
                    MessageBox.Show($"Meal added to {day}!", "Meal Added", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.DialogResult = DialogResult.OK;
                }
            };
            var btnC = new Button { Text = "Cancel", Bounds = new Rectangle(20, 148, 100, 36), FlatStyle = FlatStyle.Flat, BackColor = UI.BgCancel, ForeColor = UI.TextMid, Font = new Font("Segoe UI", 10), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            btnC.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { lDay, cboDay, lType, cboType, lName, txtName, btnAdd, btnC });
            dlg.AcceptButton = btnAdd; dlg.CancelButton = btnC;
            dlg.ShowDialog(this);
        }

        void QuickUpdateWeight()
        {
            using var dlg = new Form
            {
                Text = "Update Weight", ClientSize = new Size(320, 178),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, BackColor = UI.BgDialog
            };
            var lbl = new Label { Text = "Enter today's weight (kg):", AutoSize = true, Location = new Point(20, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid };
            var txt = new TextBox { Bounds = new Rectangle(20, 44, 140, 28), Font = new Font("Segoe UI", 12, FontStyle.Bold), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. 72.5" };
            var lNotes = new Label { Text = "Notes:", AutoSize = true, Location = new Point(20, 84), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid };
            var txtN   = new TextBox { Bounds = new Rectangle(20, 106, 280, 26), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Optional" };

            var btnSave = new Button { Text = "Save", Bounds = new Rectangle(196, 44, 104, 34), FlatStyle = FlatStyle.Flat, BackColor = UI.Green, ForeColor = Color.White, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => {
                if (double.TryParse(txt.Text, out double wt) && wt > 0)
                {
                    AppState.History.SaveToday(wt, txtN.Text.Trim());
                    RefreshGoal();
                    dlg.DialogResult = DialogResult.OK;
                }
                else MessageBox.Show("Enter a valid weight.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            };
            var btnC = new Button { Text = "Cancel", Bounds = new Rectangle(20, 140, 100, 28), FlatStyle = FlatStyle.Flat, BackColor = UI.BgCancel, ForeColor = UI.TextMid, Font = new Font("Segoe UI", 9), Cursor = Cursors.Hand, DialogResult = DialogResult.Cancel };
            btnC.FlatAppearance.BorderSize = 0;

            dlg.Controls.AddRange(new Control[] { lbl, txt, lNotes, txtN, btnSave, btnC });
            dlg.AcceptButton = btnSave; dlg.CancelButton = btnC;
            dlg.ShowDialog(this);
        }

        // ═════════════════════════════════════════════════════════════════
        //  FEATURE 3 & 4 — Streak + Goal cards in one row
        // ═════════════════════════════════════════════════════════════════
        Panel BuildStreakAndGoalRow()
        {
            var row = new Panel { Dock = DockStyle.Top, Height = 138, BackColor = UI.BgPage };

            var streakCard = BuildStreakCard();
            var goalCard   = BuildGoalCard();

            row.Controls.Add(streakCard);
            row.Controls.Add(goalCard);

            row.SizeChanged += (s, e) =>
            {
                int w   = row.ClientSize.Width;
                int gap = 14;
                int lw  = (int)(w * 0.38) - gap;
                int rw  = w - lw - gap - 36;  // 18px margin each side
                streakCard.Bounds = new Rectangle(18,       0, lw, 126);
                goalCard.Bounds   = new Rectangle(18 + lw + gap, 0, rw, 126);
            };

            return row;
        }

        // Feature 3 — Streak card
        Panel BuildStreakCard()
        {
            var card = new Panel { BackColor = UI.BgCard };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                // Gradient background strip at top
                using (var br = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 40),
                    Color.FromArgb(30, UI.Gold.R, UI.Gold.G, UI.Gold.B),
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                    g.FillRectangle(br, 0, 0, card.Width, 40);
            };

            var lHead = UI.MakeLabel("🔥  Workout Streak", 9f, FontStyle.Bold, UI.Gold);
            lHead.Location = new Point(14, 10);

            lblStreakNum = UI.MakeLabel("—", 36, FontStyle.Bold, UI.Gold, false);
            lblStreakNum.Location = new Point(14, 34);
            lblStreakNum.Size     = new Size(80, 50);

            var lUnit = UI.MakeLabel("days in a row", 9f, FontStyle.Regular, UI.TextMid);
            lUnit.Location = new Point(100, 56);

            var lTip = UI.MakeLabel("Log workouts daily to grow your streak!", 8f, FontStyle.Regular, UI.TextFaded);
            lTip.Location = new Point(14, 96);
            lTip.AutoSize = false;
            lTip.Size     = new Size(card.Width - 28, 18);
            lTip.Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            card.Controls.AddRange(new Control[] { lHead, lblStreakNum, lUnit, lTip });
            RefreshStreak();
            return card;
        }

        int ComputeStreak()
        {
            int streak = 0;
            var today = DateTime.Today;
            for (int i = 0; i < 30; i++)
            {
                var check = today.AddDays(-i);
                var log   = AppState.History.Logs.Find(l => l.Date.Date == check.Date);
                if (log != null && log.ExercisesCompleted > 0) streak++;
                else if (i > 0) break;  // gap found — stop (day 0 = today, may not be logged yet)
            }
            return streak;
        }

        void RefreshStreak()
        {
            if (lblStreakNum == null) return;
            int s = ComputeStreak();
            lblStreakNum.Text      = s.ToString();
            lblStreakNum.ForeColor = s >= 7 ? UI.Green : s >= 3 ? UI.Gold : UI.Orange;
        }

        // Feature 4 — Goal Progress card
        Panel BuildGoalCard()
        {
            var card = new Panel { BackColor = UI.BgCard };

            card.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var br = new LinearGradientBrush(
                    new Rectangle(0, 0, card.Width, 40),
                    Color.FromArgb(30, UI.Green.R, UI.Green.G, UI.Green.B),
                    Color.Transparent,
                    LinearGradientMode.Vertical))
                    g.FillRectangle(br, 0, 0, card.Width, 40);
            };

            var lHead = UI.MakeLabel("⚖  Weight Goal Progress", 9f, FontStyle.Bold, UI.Green);
            lHead.Location = new Point(14, 10);

            lblGoalCurrent = UI.MakeLabel("Current: — kg", 10.5f, FontStyle.Bold, UI.TextDark);
            lblGoalCurrent.Location = new Point(14, 34);

            lblGoalTarget = UI.MakeLabel("Target: — kg", 9f, FontStyle.Regular, UI.TextMid);
            lblGoalTarget.Location = new Point(14, 58);

            barGoal = new ProgressBar
            {
                Bounds  = new Rectangle(14, 78, 260, 10),
                Minimum = 0, Maximum = 100,
                Style   = ProgressBarStyle.Continuous,
                ForeColor = UI.Green,
                Anchor  = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            lblGoalDiff = UI.MakeLabel("Log your weight to track progress", 8f, FontStyle.Regular, UI.TextFaded);
            lblGoalDiff.Location = new Point(14, 96);
            lblGoalDiff.AutoSize = false;
            lblGoalDiff.Size     = new Size(card.Width - 28, 18);
            lblGoalDiff.Anchor   = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            card.SizeChanged += (s, e) =>
            {
                barGoal.Width      = card.Width - 28;
                lblGoalDiff.Width  = card.Width - 28;
            };

            card.Controls.AddRange(new Control[] { lHead, lblGoalCurrent, lblGoalTarget, barGoal, lblGoalDiff });
            RefreshGoal();
            return card;
        }

        void RefreshGoal()
        {
            if (lblGoalCurrent == null) return;
            double avg = AppState.History.AverageWeight();
            double target = 75.0;  // default target — could be user-configurable
            if (avg > 0)
            {
                lblGoalCurrent.Text = $"Current: {avg:F1} kg";
                lblGoalTarget.Text  = $"Target:  {target:F1} kg";
                double pct = avg <= target ? 100 : Math.Max(0, 100 - ((avg - target) / target * 100));
                barGoal.Value = Math.Min(100, (int)pct);
                double diff = avg - target;
                lblGoalDiff.Text      = diff > 0.5 ? $"{diff:F1} kg above target — keep going!"
                                      : diff < -0.5 ? $"You are {Math.Abs(diff):F1} kg below target!"
                                      :               "You are right on target! 🎯";
                lblGoalDiff.ForeColor = diff > 0.5 ? UI.Orange : UI.Green;
            }
            else
            {
                lblGoalCurrent.Text   = "Current: — kg";
                lblGoalTarget.Text    = $"Target:  {target:F1} kg";
                lblGoalDiff.Text      = "Log your weight to track progress";
                lblGoalDiff.ForeColor = UI.TextFaded;
                barGoal.Value         = 0;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        //  FEATURE 2 — Weekly Progress Chart (pure GDI+ — no NuGet needed)
        // ═════════════════════════════════════════════════════════════════
        Panel BuildWeeklyChart()
        {
            var outer = new Panel { Dock = DockStyle.Top, Height = 268, BackColor = UI.BgPage };
            var lblTitle = UI.MakeLabel("Weekly Progress Chart", 11, FontStyle.Bold, UI.TextDark);
            lblTitle.Location = new Point(20, 8);
            outer.Controls.Add(lblTitle);

            var card = new Panel
            {
                BackColor = UI.BgCard,
                Bounds    = new Rectangle(18, 32, outer.Width - 36, 226),
                Anchor    = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };
            outer.SizeChanged += (s, e) => card.Width = outer.Width - 36;

            // The chart is a custom-painted panel — zero dependencies
            var chartPanel = new Panel { Dock = DockStyle.Fill, BackColor = UI.BgCard };
            chartPanel.Paint += DrawGdiChart;

            // Re-draw whenever data changes
            AppState.StatsChanged += () =>
            {
                if (chartPanel.IsDisposed) return;
                if (chartPanel.InvokeRequired) chartPanel.Invoke(new Action(chartPanel.Invalidate));
                else chartPanel.Invalidate();
            };

            card.Controls.Add(chartPanel);
            outer.Controls.Add(card);
            return outer;
        }

        // Draws a bar + line chart using only System.Drawing (no NuGet)
        void DrawGdiChart(object? sender, PaintEventArgs e)
        {
            var g     = e.Graphics;
            var panel = (Panel)sender!;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // ── Collect 7 days of data ────────────────────────────────────
            int days = 7;
            int[]    exData  = new int[days];
            double[] calData = new double[days];
            string[] labels  = new string[days];
            for (int i = 0; i < days; i++)
            {
                var d   = DateTime.Today.AddDays(-(days - 1 - i));
                var log = AppState.History.Logs.Find(l => l.Date.Date == d.Date);
                labels[i]  = d.ToString("ddd");
                exData[i]  = log?.ExercisesCompleted ?? 0;
                calData[i] = log?.CaloriesConsumed    ?? 0;
            }

            // ── Layout ───────────────────────────────────────────────────
            const int padL = 54, padR = 20, padT = 22, padB = 46;
            int chartW = panel.Width  - padL - padR;
            int chartH = panel.Height - padT - padB;
            if (chartW < 10 || chartH < 10) return;

            var chartRect = new Rectangle(padL, padT, chartW, chartH);

            // ── Background ───────────────────────────────────────────────
            g.FillRectangle(new SolidBrush(UI.BgCard), panel.ClientRectangle);

            // Subtle horizontal grid lines
            int maxEx  = Math.Max(1, Array.FindAll(exData, v => v > 0).Length > 0
                                     ? exData.Max() + 1 : 5);
            double maxCal = Math.Max(1, calData.Max() > 0 ? calData.Max() * 1.15 : 2500);
            int gridLines = 4;
            using var gridPen = new Pen(Color.FromArgb(18, 0, 0, 0), 1);
            var labelFont = new Font("Segoe UI", 7.5f);

            for (int i = 0; i <= gridLines; i++)
            {
                int gy = padT + (int)(chartH * i / (double)gridLines);
                g.DrawLine(gridPen, padL, gy, padL + chartW, gy);

                // Left Y-axis label (exercises)
                double exVal = maxEx * (gridLines - i) / (double)gridLines;
                using (var b = new SolidBrush(UI.TextFaded))
                    g.DrawString(exVal.ToString("F0"), labelFont, b,
                        2, gy - 7);

                // Right Y-axis label (calories)
                double calVal = maxCal * (gridLines - i) / (double)gridLines;
                string calText = calVal >= 1000 ? $"{calVal / 1000:F1}k" : calVal.ToString("F0");
                using (var b = new SolidBrush(UI.TextFaded))
                    g.DrawString(calText, labelFont, b,
                        padL + chartW + 4, gy - 7);
            }

            // Chart border
            using var borderPen = new Pen(UI.Border);
            g.DrawRectangle(borderPen, chartRect);

            // ── Bars (exercises) ─────────────────────────────────────────
            int barAreaW = chartW / days;
            int barW     = Math.Max(8, barAreaW - 12);

            for (int i = 0; i < days; i++)
            {
                int bx    = padL + i * barAreaW + (barAreaW - barW) / 2;
                double pct= exData[i] / (double)maxEx;
                int barH  = (int)(chartH * pct);
                int by    = padT + chartH - barH;

                if (barH > 0)
                {
                    // Bar fill with gradient tint
                    Color barColor = exData[i] > 0
                        ? Color.FromArgb(200, UI.Cyan.R, UI.Cyan.G, UI.Cyan.B)
                        : Color.FromArgb(40,  UI.Cyan.R, UI.Cyan.G, UI.Cyan.B);
                    g.FillRectangle(new SolidBrush(barColor), bx, by, barW, barH);

                    // Value on top of bar
                    if (exData[i] > 0)
                        using (var b = new SolidBrush(UI.Cyan))
                            g.DrawString(exData[i].ToString(),
                                new Font("Segoe UI", 7.5f, FontStyle.Bold), b,
                                bx + barW / 2 - 5, by - 15);
                }

                // Day label below chart
                using (var b = new SolidBrush(
                    DateTime.Today.AddDays(-(days - 1 - i)).Date == DateTime.Today.Date
                        ? UI.Cyan : UI.TextMid))
                    g.DrawString(labels[i], labelFont, b,
                        padL + i * barAreaW + (barAreaW - 26) / 2,
                        padT + chartH + 8);
            }

            // ── Calorie line ──────────────────────────────────────────────
            var linePoints = new System.Collections.Generic.List<PointF>();
            for (int i = 0; i < days; i++)
            {
                float lx = padL + i * barAreaW + barAreaW / 2f;
                float ly = padT + chartH - (float)(calData[i] / maxCal * chartH);
                linePoints.Add(new PointF(lx, ly));
            }

            if (linePoints.Count >= 2)
            {
                using var linePen = new Pen(UI.Orange, 2.5f);
                linePen.LineJoin = System.Drawing.Drawing2D.LineJoin.Round;
                g.DrawLines(linePen, linePoints.ToArray());

                // Dots on each data point
                foreach (var pt in linePoints)
                {
                    g.FillEllipse(new SolidBrush(UI.Orange), pt.X - 4, pt.Y - 4, 8, 8);
                    g.FillEllipse(new SolidBrush(UI.BgCard),  pt.X - 2, pt.Y - 2, 4, 4);
                }
            }

            // ── Legend ────────────────────────────────────────────────────
            int lx0 = padL + 4, ly0 = padT + chartH + 28;
            g.FillRectangle(new SolidBrush(Color.FromArgb(200, UI.Cyan.R, UI.Cyan.G, UI.Cyan.B)),
                lx0, ly0, 14, 10);
            using (var b = new SolidBrush(UI.TextMid))
                g.DrawString("Exercises completed", labelFont, b, lx0 + 18, ly0 - 1);

            lx0 += 160;
            g.DrawLine(new Pen(UI.Orange, 2.5f), lx0, ly0 + 5, lx0 + 14, ly0 + 5);
            g.FillEllipse(new SolidBrush(UI.Orange), lx0 + 3, ly0 + 2, 8, 8);
            using (var b = new SolidBrush(UI.TextMid))
                g.DrawString("Calories consumed", labelFont, b, lx0 + 18, ly0 - 1);
        }

        // ═════════════════════════════════════════════════════════════════
        //  FEATURE 6 — Section 5: Workout plan + Meal summary (two columns)
        // ═════════════════════════════════════════════════════════════════
        Panel BuildBodyRow()
        {
            var bodyPanel = new Panel
            {
                Dock      = DockStyle.Top,
                BackColor = UI.BgPage,
                Padding   = new Padding(18, 6, 18, 6)
            };
            bodyPanel.Height = Math.Max(
                wp.Days.Count * 58 + 60,
                (dp.Week.Count > 0 ? dp.Week[0].Meals.Count : 0) * 56 + 60);

            // Left — workout plan
            var leftCard = new Panel { BackColor = UI.BgCard };
            var wHdr = SectionHdr("This Week's Workout Plan", UI.Cyan);
            wHdr.Dock = DockStyle.None; wHdr.Location = new Point(0, 0);
            int wy = 46;
            foreach (var day in wp.Days)
            {
                var row = WorkoutDayRow(day);
                row.Location = new Point(0, wy);
                leftCard.Controls.Add(row);
                wy += 58;
            }
            leftCard.Height = wy + 8;
            leftCard.Controls.Add(wHdr);
            leftCard.Controls.Add(new Panel { BackColor = UI.Border, Bounds = new Rectangle(0, 44, 2000, 1) });

            // Right — meal summary
            var rightCard = new Panel { BackColor = UI.BgCard };
            var mHdr = SectionHdr("Today's Meal Summary", UI.Orange);
            mHdr.Dock = DockStyle.None; mHdr.Location = new Point(0, 0);
            int my = 46;
            var todayPlan = dp.Week.Count > 0 ? dp.Week[0] : null;
            if (todayPlan != null)
                foreach (var meal in todayPlan.Meals)
                {
                    var row = MealSummaryRow(meal);
                    row.Location = new Point(0, my);
                    rightCard.Controls.Add(row);
                    my += 56;
                }
            rightCard.Height = my + 8;
            rightCard.Controls.Add(mHdr);
            rightCard.Controls.Add(new Panel { BackColor = UI.Border, Bounds = new Rectangle(0, 44, 2000, 1) });

            bodyPanel.Controls.Add(leftCard);
            bodyPanel.Controls.Add(rightCard);

            bodyPanel.SizeChanged += (s, e) =>
            {
                int w   = bodyPanel.ClientSize.Width;
                int gap = 10;
                int lw  = (w - gap) / 2;
                int rw  = w - lw - gap;
                leftCard.Bounds  = new Rectangle(0,       0, lw, leftCard.Height);
                rightCard.Bounds = new Rectangle(lw + gap, 0, rw, rightCard.Height);
                foreach (Control c in leftCard.Controls)
                    if (c is Panel p && p != wHdr) p.Width = lw;
                foreach (Control c in rightCard.Controls)
                    if (c is Panel p && p != mHdr) p.Width = rw;
                bodyPanel.Height = Math.Max(leftCard.Height, rightCard.Height) + 12;
            };

            return bodyPanel;
        }

        // ═════════════════════════════════════════════════════════════════
        //  HELPERS  (kept exactly as before)
        // ═════════════════════════════════════════════════════════════════

        Panel SectionHdr(string title, Color accent)
        {
            var p = new Panel { Height = 44, BackColor = UI.BgHeader, Width = 800 };
            p.Controls.Add(new Panel { BackColor = accent, Width = 4, Dock = DockStyle.Left });
            var lbl = UI.MakeLabel(title, 11, FontStyle.Bold, UI.TextDark);
            lbl.Location = new Point(18, 12);
            p.Controls.Add(lbl);
            return p;
        }

        Panel WorkoutDayRow(WorkoutDay day)
        {
            var p = new Panel { Height = 56, BackColor = UI.BgCard, Cursor = Cursors.Hand };
            p.Controls.Add(new Panel { BackColor = UI.Cyan, Width = 4, Dock = DockStyle.Left });
            var lDow  = UI.MakeLabel(day.DayOfWeek.ToString().Substring(0, 3).ToUpper(), 7.5f, FontStyle.Bold, UI.Cyan); lDow.Location = new Point(14, 6);
            var lName = UI.MakeLabel(day.DayLabel, 9.5f, FontStyle.Bold, UI.TextDark); lName.Location = new Point(14, 23);
            var lInfo = UI.MakeLabel($"{day.Exercises.Count} exercises  •  {day.Difficulty}", 8.5f, FontStyle.Regular, UI.TextMid); lInfo.Location = new Point(320, 20);
            var sep   = new Panel { BackColor = UI.Border, Height = 1, Dock = DockStyle.Bottom };
            p.Controls.AddRange(new Control[] { lDow, lName, lInfo, sep });
            AddRowHover(p, UI.BgCard, UI.TileHover);
            return p;
        }

        Panel MealSummaryRow(Meal meal)
        {
            Color c = meal.Type == MealType.Breakfast ? UI.Gold
                    : meal.Type == MealType.Lunch     ? UI.Green
                    : meal.Type == MealType.Dinner    ? UI.Blue
                    : UI.Purple;
            var p = new Panel { Height = 54, BackColor = UI.BgCard, Cursor = Cursors.Hand };
            p.Controls.Add(new Panel { BackColor = c, Width = 4, Dock = DockStyle.Left });
            var lT    = UI.MakeLabel(meal.Type.ToString(), 7.5f, FontStyle.Bold, c); lT.Location = new Point(14, 5);
            var lName = UI.MakeLabel(meal.ToString(), 9.5f, FontStyle.Bold, UI.TextDark); lName.Location = new Point(14, 22);
            var lCal  = UI.MakeLabel($"{meal.TotalCalories:N0} kcal", 9f, FontStyle.Bold, UI.Orange); lCal.Location = new Point(260, 18);
            var sep   = new Panel { BackColor = UI.Border, Height = 1, Dock = DockStyle.Bottom };
            p.Controls.AddRange(new Control[] { lT, lName, lCal, sep });
            AddRowHover(p, UI.BgCard, UI.TileHover);
            return p;
        }

        // Shared smooth hover fade helper used by rows and cards
        static void AddRowHover(Panel p, Color normal, Color hovered)
        {
            var t       = new System.Windows.Forms.Timer { Interval = 16 };
            bool isOver = false;
            float blend = 0f;
            t.Tick += (ts, te) =>
            {
                float target = isOver ? 1f : 0f;
                blend += (target - blend) * 0.22f;
                if (Math.Abs(blend - target) < 0.02f) { blend = target; t.Stop(); }
                int r  = (int)(normal.R + (hovered.R - normal.R) * blend);
                int g2 = (int)(normal.G + (hovered.G - normal.G) * blend);
                int b2 = (int)(normal.B + (hovered.B - normal.B) * blend);
                if (!p.IsDisposed) p.BackColor = Color.FromArgb(r, g2, b2);
            };
            // Propagate hover from child controls
            void Enter(object? s, EventArgs e) { isOver = true;  t.Start(); }
            void Leave(object? s, EventArgs e) { isOver = false; t.Start(); }
            p.MouseEnter += Enter; p.MouseLeave += Leave;
            foreach (Control child in p.Controls)
            { child.MouseEnter += Enter; child.MouseLeave += Leave; }
        }

        // ── BMI Calculator (unchanged) ────────────────────────────────────
        Panel BuildBmiCard()
        {
            var outer = new Panel { Dock = DockStyle.Top, Height = 290, BackColor = UI.BgPage, Padding = new Padding(18, 0, 18, 10) };
            var lT = UI.MakeLabel("BMI & Nutrition Calculator", 13, FontStyle.Bold, UI.TextDark); lT.Location = new Point(18, 8);
            outer.Controls.Add(lT);

            var card = new Panel { BackColor = UI.BgCard, Bounds = new Rectangle(18, 36, outer.Width - 36, 244) };
            card.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            outer.SizeChanged += (s, e) => card.Width = outer.Width - 36;

            int ix = 14, iy = 14;
            void IL(string t, int x) { var l = UI.MakeLabel(t, 8.5f, FontStyle.Bold, UI.TextMid); l.Location = new Point(x, iy); card.Controls.Add(l); }
            TextBox IT(int x, int w, string def) { var tb = new TextBox { Bounds = new Rectangle(x, iy + 20, w, 26), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, Text = def }; card.Controls.Add(tb); return tb; }

            IL("Weight (kg):", ix); var txtW   = IT(ix, 88, "70");  ix += 106;
            IL("Height (cm):", ix); var txtH   = IT(ix, 88, "175"); ix += 106;
            IL("Age:", ix);         var txtAge = IT(ix, 56, "25");  ix += 74;
            IL("Gender:", ix);
            var cboGen = new ComboBox { Bounds = new Rectangle(ix, iy + 20, 90, 26), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            cboGen.Items.AddRange(new object[] { "Male", "Female" }); cboGen.SelectedIndex = 0;
            card.Controls.Add(cboGen); ix += 108;
            var btnC = UI.MakeBtn("Calculate", UI.Cyan, UI.TextDark, 100, 28, 9f);
            btnC.Location = new Point(ix, iy + 18); card.Controls.Add(btnC);

            iy += 62;
            card.Controls.Add(new Panel { BackColor = UI.Border, Bounds = new Rectangle(14, iy, 560, 1) }); iy += 10;

            var lblBmi = UI.MakeLabel("BMI: —", 14, FontStyle.Bold, UI.Cyan, false); lblBmi.Bounds = new Rectangle(14, iy, 130, 26);
            var lblCat = UI.MakeLabel("", 9, FontStyle.Bold, UI.TextMid, false); lblCat.Bounds  = new Rectangle(150, iy + 4, 200, 20);
            iy += 34;
            var lblRC  = UI.MakeLabel("Recommended calories: —", 9.5f, FontStyle.Regular, UI.TextDark, false); lblRC.Bounds = new Rectangle(14, iy, 500, 20);
            iy += 26;
            var lblRP  = UI.MakeLabel("Recommended protein: —",  9.5f, FontStyle.Regular, UI.TextDark, false); lblRP.Bounds = new Rectangle(14, iy, 500, 20);
            iy += 28;
            var lblAdv = UI.MakeLabel("", 8.5f, FontStyle.Regular, UI.TextMid, false);
            lblAdv.Bounds = new Rectangle(14, iy, 540, 40); 

            card.Controls.AddRange(new Control[] { lblBmi, lblCat, lblRC, lblRP, lblAdv });

            btnC.Click += (s, e) => {
                if (!double.TryParse(txtW.Text, out double w) || w <= 0 ||
                    !double.TryParse(txtH.Text, out double h) || h <= 0 ||
                    !int.TryParse(txtAge.Text,  out int age) || age <= 0)
                { MessageBox.Show("Enter valid weight, height and age.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
                bool isMale = cboGen.SelectedIndex == 0;
                double bmi  = BmiCalculator.Calculate(w, h);
                lblBmi.Text = $"BMI: {bmi:F1}"; lblBmi.ForeColor = BmiCalculator.CategoryColor(bmi);
                lblCat.Text = BmiCalculator.Category(bmi); lblCat.ForeColor = BmiCalculator.CategoryColor(bmi);
                lblRC.Text  = $"Recommended calories:   {BmiCalculator.RecommendedCalories(w, h, age, isMale):N0} kcal / day";
                lblRP.Text  = $"Recommended protein:     {BmiCalculator.RecommendedProtein(w):N1} g / day";
                lblAdv.Text = BmiCalculator.Advice(bmi);
            };

            outer.Controls.Add(card);
            return outer;
        }

        // ── Progress Tracker (unchanged) ──────────────────────────────────
        Panel BuildProgressCard()
        {
            var outer = new Panel { Dock = DockStyle.Top, Height = 300, BackColor = UI.BgPage, Padding = new Padding(18, 0, 18, 18) };
            var lT = UI.MakeLabel("Weekly Progress Tracker", 13, FontStyle.Bold, UI.TextDark); lT.Location = new Point(18, 8);
            outer.Controls.Add(lT);

            var card = new Panel { BackColor = UI.BgCard, Bounds = new Rectangle(18, 36, outer.Width - 36, 256) };
            card.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            outer.SizeChanged += (s, e) => card.Width = outer.Width - 36;

            var lW    = UI.MakeLabel("Today's weight (kg):", 8.5f, FontStyle.Bold, UI.TextMid); lW.Location = new Point(14, 14);
            var txtWt = new TextBox { Bounds = new Rectangle(14, 34, 114, 26), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "e.g. 72.5" };
            var lN    = UI.MakeLabel("Notes:", 8.5f, FontStyle.Bold, UI.TextMid); lN.Location = new Point(144, 14);
            var txtN  = new TextBox { Bounds = new Rectangle(144, 34, 204, 26), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = "Optional note" };
            var btnSave = UI.MakeBtn("Save Today", UI.Green, Color.White, 106, 28, 9f); btnSave.Location = new Point(364, 33);

            var lblSum = UI.MakeLabel("", 8.5f, FontStyle.Regular, UI.TextMid, false);
            lblSum.Bounds = new Rectangle(14, 70, card.Width - 28, 20);
            lblSum.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            var lH = UI.MakeLabel("Last 7 days:", 8.5f, FontStyle.Bold, UI.TextMid); lH.Location = new Point(14, 96);

            var grid = new ListView
            {
                Bounds = new Rectangle(14, 116, card.Width - 28, 122),
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
                View = View.Details, FullRowSelect = true, GridLines = true,
                Font = new Font("Segoe UI", 9), BackColor = UI.BgPage,
                ForeColor = UI.TextDark, BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            grid.Columns.Add("Date", 90); grid.Columns.Add("Weight", 72); grid.Columns.Add("Exercises", 76);
            grid.Columns.Add("Calories", 76); grid.Columns.Add("Protein (g)", 82); grid.Columns.Add("Notes", 130);

            card.SizeChanged += (s, e) =>
            {
                int w2 = card.Width - 28; grid.Width = w2; lblSum.Width = w2;
                int used = 0; for (int i = 0; i < grid.Columns.Count - 1; i++) used += grid.Columns[i].Width;
                grid.Columns[grid.Columns.Count - 1].Width = Math.Max(60, w2 - used - 4);
            };

            Action refresh = () =>
            {
                grid.Items.Clear();
                foreach (var log in AppState.History.LastSevenDays())
                {
                    var item = new ListViewItem(log.Date.ToString("ddd MMM d"));
                    item.SubItems.Add(log.WeightKg > 0 ? log.WeightKg.ToString("F1") : "—");
                    item.SubItems.Add(log.ExercisesCompleted.ToString());
                    item.SubItems.Add($"{log.CaloriesConsumed:N0}");
                    item.SubItems.Add($"{log.ProteinConsumed:N0}");
                    item.SubItems.Add(log.Notes);
                    if (log.Date.Date == DateTime.Today) item.BackColor = Color.FromArgb(22, 0, 180, 215);
                    grid.Items.Add(item);
                }
                var h = AppState.History;
                double avgW = h.AverageWeight(), trend = h.WeightTrend();
                lblSum.Text = $"Avg weight: {(avgW > 0 ? avgW.ToString("F1") + " kg" : "—")}   "
                            + $"Trend: {(avgW > 0 ? (trend >= 0 ? "+" : "") + trend.ToString("F1") + " kg" : "—")}   "
                            + $"Exercises/week: {h.TotalExercisesThisWeek()}   "
                            + $"Avg kcal/day: {(h.AverageCaloriesThisWeek() > 0 ? h.AverageCaloriesThisWeek().ToString("N0") : "—")}";
            };

            btnSave.Click += (s, e) =>
            {
                double.TryParse(txtWt.Text, out double wt);
                AppState.History.SaveToday(wt, txtN.Text.Trim());
                refresh(); RefreshGoal();
                txtWt.Clear(); txtN.Clear();
            };

            refresh();
            card.Controls.AddRange(new Control[] { lW, txtWt, lN, txtN, btnSave, lblSum, lH, grid });
            outer.Controls.Add(card);
            return outer;
        }

        int GetWeekNum()
        {
            var cal = System.Globalization.CultureInfo.CurrentCulture.Calendar;
            return cal.GetWeekOfYear(DateTime.Today,
                System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }
    }
}
