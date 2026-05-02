using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitPlannerApp
{
    public class WorkoutPlannerPanel : Panel
    {
        WorkoutPlan  plan;
        WorkoutDay?  selDay;
        Exercise?    selEx;

        ListBox lstDays, lstExercises;

        // Detail controls
        Label    detName, detCategory, detSummary, detPlaceholderLbl;
        TextBox  detNotes;
        Button   btnSaveNotes, btnToggleDone, btnDeleteEx;

        // Feature 1 — progress tracking
        Label       lblProgress;
        ProgressBar barProgress;

        public WorkoutPlannerPanel(WorkoutPlan workout)
        {
            plan = workout;
            BackColor = UI.BgPage;
            Build();
        }

        // ─────────────────────────────────────────────────────────────────────
        void Build()
        {
            // ── Page header ───────────────────────────────────────────────────
            var topBar = new Panel { Dock = DockStyle.Top, Height = 72, BackColor = UI.BgPage };
            var lblTitle = UI.MakeLabel("Workout Editor", 20, FontStyle.Bold, UI.TextDark);
            lblTitle.Location = new Point(24, 12);
            var lblSub = UI.MakeLabel("Plan: " + plan.PlanName, 9f, FontStyle.Regular, UI.TextMid);
            lblSub.Location = new Point(24, 46);
            var btnAddDay = UI.MakeBtn("+ Add Workout Day", UI.Cyan, UI.TextDark, 190, 38);
            btnAddDay.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnAddDay.Location = new Point(topBar.Width - 210, 17);
            topBar.SizeChanged += (s, e) => btnAddDay.Location = new Point(topBar.Width - 210, 17);
            btnAddDay.Click += OnAddDay;
            topBar.Controls.AddRange(new Control[] { lblTitle, lblSub, btnAddDay });
            Controls.Add(topBar);

            // ── Three-column outer layout ─────────────────────────────────────
            var outer = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 3,
                RowCount        = 1,
                BackColor       = UI.BgPage,
                Padding         = new Padding(14, 4, 14, 14),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37f));
            Controls.Add(outer);

            outer.Controls.Add(BuildDaysCol(),      0, 0);
            outer.Controls.Add(BuildExercisesCol(), 1, 0);
            outer.Controls.Add(BuildDetailCol(),    2, 0);
        }

        // ── SHARED: inner TableLayoutPanel for a column ───────────────────────
        // rows: [header][buttons][sep][extra?][list]
        TableLayoutPanel ColTLP(int btnH, bool hasExtraRow, out int listRow)
        {
            int rows = hasExtraRow ? 5 : 4;
            listRow  = rows - 1;
            var t = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 1,
                RowCount        = rows,
                BackColor       = UI.BgCard,
                Margin          = new Padding(4),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0)
            };
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // header
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, btnH)); // buttons
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));    // separator
            if (hasExtraRow)
                t.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // progress
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // list
            return t;
        }

        Panel ColHeaderPanel(string title, Color accent)
        {
            var p   = new Panel { Dock = DockStyle.Fill, BackColor = UI.BgHeader };
            var bar = new Panel { BackColor = accent, Width = 4, Dock = DockStyle.Left };
            var lbl = UI.MakeLabel(title, 11, FontStyle.Bold, UI.TextDark);
            lbl.Location = new Point(16, 13);
            p.Controls.Add(bar);
            p.Controls.Add(lbl);
            return p;
        }

        Panel ColBtnPanel(params (string text, Color bg, Color fg, EventHandler click)[] buttons)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = UI.BgCard };
            int x = 8;
            foreach (var (text, bg, fg, click) in buttons)
            {
                int w = text.Length > 8 ? 128 : 90;
                var b = UI.MakeBtn(text, bg, fg, w, 32, 8.5f);
                b.Location = new Point(x, 8);
                b.Click   += click;
                p.Controls.Add(b);
                x += w + 8;
            }
            return p;
        }

        // ── COLUMN 1: Days ────────────────────────────────────────────────────
        TableLayoutPanel BuildDaysCol()
        {
            var t = ColTLP(50, false, out int listRow);

            t.Controls.Add(ColHeaderPanel("Workout Days", UI.Cyan), 0, 0);
            t.Controls.Add(ColBtnPanel(
                ("+ Add Day", UI.Cyan, UI.TextDark, OnAddDay),
                ("Remove",    UI.Red,  Color.White,  OnRemoveDay)), 0, 1);
            t.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UI.Border }, 0, 2);

            lstDays = new ListBox
            {
                Dock        = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor   = UI.BgCard,
                ItemHeight  = 58,
                DrawMode    = DrawMode.OwnerDrawFixed
            };
            lstDays.DrawItem            += DrawDay;
            lstDays.SelectedIndexChanged += OnDaySelected;
            foreach (var d in plan.Days) lstDays.Items.Add(d);
            t.Controls.Add(lstDays, 0, listRow);
            return t;
        }

        // ── COLUMN 2: Exercises ───────────────────────────────────────────────
        TableLayoutPanel BuildExercisesCol()
        {
            var t = ColTLP(50, true, out int listRow);

            t.Controls.Add(ColHeaderPanel("Activities", UI.Blue), 0, 0);
            t.Controls.Add(ColBtnPanel(
                ("+ Add Exercise", UI.Cyan, UI.TextDark, OnAddExercise),
                ("Remove",         UI.Red,  Color.White,  OnRemoveExercise)), 0, 1);
            t.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UI.Border }, 0, 2);

            // Progress bar row (row 3)
            var progPanel = new Panel { Dock = DockStyle.Fill, BackColor = UI.BgCard, Padding = new Padding(10, 6, 10, 4) };
            lblProgress = UI.MakeLabel("Select a day to see progress", 8.5f, FontStyle.Regular, UI.TextMid, false);
            lblProgress.Dock = DockStyle.Top;
            lblProgress.Height = 16;
            barProgress = new ProgressBar
            {
                Dock      = DockStyle.Top,
                Height    = 10,
                Minimum   = 0,
                Maximum   = 100,
                Style     = ProgressBarStyle.Continuous,
                ForeColor = UI.Green
            };
            // Dock order reversed: last added = first docked (Top)
            progPanel.Controls.Add(barProgress);
            progPanel.Controls.Add(lblProgress);
            t.Controls.Add(progPanel, 0, 3);

            lstExercises = new ListBox
            {
                Dock        = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor   = UI.BgCard,
                ItemHeight  = 66,
                DrawMode    = DrawMode.OwnerDrawFixed
            };
            lstExercises.DrawItem            += DrawExercise;
            lstExercises.SelectedIndexChanged += OnExerciseSelected;
            lstExercises.MouseClick           += OnExerciseListClick;
            t.Controls.Add(lstExercises, 0, listRow);
            return t;
        }

        // ── COLUMN 3: Exercise Detail ─────────────────────────────────────────
        TableLayoutPanel BuildDetailCol()
        {
            var t = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 1,
                RowCount        = 3,
                BackColor       = UI.BgCard,
                Margin          = new Padding(4),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Padding         = new Padding(0)
            };
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // header
            t.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));    // sep
            t.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // content

            t.Controls.Add(ColHeaderPanel("Exercise Detail", UI.Green), 0, 0);
            t.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UI.Border }, 0, 1);

            // Scrollable detail content
            var scroll = new Panel { Dock = DockStyle.Fill, BackColor = UI.BgCard, AutoScroll = true, Padding = new Padding(16, 14, 16, 14) };
            BuildDetailControls(scroll);
            t.Controls.Add(scroll, 0, 2);
            return t;
        }

        void BuildDetailControls(Panel p)
        {
            detPlaceholderLbl = UI.MakeLabel(
                "← Select an exercise\nfrom the list to view\nand edit its details.",
                10, FontStyle.Regular, UI.TextFaded, false);
            detPlaceholderLbl.Location = new Point(0, 20);
            detPlaceholderLbl.Size     = new Size(280, 60);

            detName = UI.MakeLabel("", 15, FontStyle.Bold, UI.TextDark, false);
            detName.Location = new Point(0, 4);
            detName.Size     = new Size(280, 32);

            detCategory = UI.MakeLabel("", 9f, FontStyle.Regular, UI.Cyan);
            detCategory.Location = new Point(0, 42);

            var div1 = new Panel { BackColor = UI.Border, Size = new Size(280, 1), Location = new Point(0, 66) };

            detSummary = UI.MakeLabel("", 9.5f, FontStyle.Regular, UI.TextDark, false);
            detSummary.Location = new Point(0, 76);
            detSummary.Size     = new Size(280, 100);

            var div2 = new Panel { BackColor = UI.Border, Size = new Size(280, 1), Location = new Point(0, 184) };

            var lNotes = UI.MakeLabel("Notes", 9f, FontStyle.Bold, UI.TextMid);
            lNotes.Location = new Point(0, 194);

            detNotes = new TextBox
            {
                Multiline   = true,
                ScrollBars  = ScrollBars.Vertical,
                Font        = new Font("Segoe UI", 9.5f),
                BackColor   = UI.BgInput,
                BorderStyle = BorderStyle.FixedSingle,
                Location    = new Point(0, 215),
                Size        = new Size(280, 78)
            };

            btnSaveNotes = UI.MakeBtn("Save Notes", UI.Cyan, UI.TextDark, 132, 32, 9f);
            btnSaveNotes.Location = new Point(0, 302);
            btnSaveNotes.Click   += (s, e) => { if (selEx != null) selEx.Notes = detNotes.Text; };

            btnToggleDone = UI.MakeBtn("Mark Done", UI.Green, Color.White, 132, 32, 9f);
            btnToggleDone.Location = new Point(148, 302);
            btnToggleDone.Click   += ToggleDone;

            btnDeleteEx = UI.MakeBtn("Delete Exercise", UI.Red, Color.White, 280, 32, 9f);
            btnDeleteEx.Location = new Point(0, 344);
            btnDeleteEx.Click   += OnRemoveExercise;

            p.Controls.AddRange(new Control[] {
                detPlaceholderLbl, detName, detCategory, div1,
                detSummary, div2, lNotes, detNotes,
                btnSaveNotes, btnToggleDone, btnDeleteEx
            });
            SetDetailVisible(false);
        }

        void SetDetailVisible(bool show)
        {
            detPlaceholderLbl.Visible = !show;
            detName.Visible           =  show;
            detCategory.Visible       =  show;
            detSummary.Visible        =  show;
            detNotes.Visible          =  show;
            btnSaveNotes.Visible      =  show;
            btnToggleDone.Visible     =  show;
            btnDeleteEx.Visible       =  show;
        }

        void LoadDetail(Exercise ex)
        {
            SetDetailVisible(true);
            detName.Text     = ex.Name;
            detCategory.Text = "Category: " + ex.Category;
            detNotes.Text    = ex.Notes ?? "";
            RefreshSummary(ex);
            RefreshToggleBtn(ex);
        }

        void RefreshSummary(Exercise ex)
        {
            string s = ex.DurationMinutes > 0
                ? $"Duration:    {ex.DurationMinutes} min\n"
                : $"Sets:          {ex.Sets}\nReps:         {ex.Reps}\n"
                  + (ex.WeightKg > 0 ? $"Weight:      {ex.WeightKg} kg\n" : "");
            s += $"\nStatus:       {(ex.IsCompleted ? "✓ Completed" : "Not done")}";
            detSummary.Text = s;
        }

        void RefreshToggleBtn(Exercise ex)
        {
            btnToggleDone.Text      = ex.IsCompleted ? "Unmark Done" : "Mark Done";
            btnToggleDone.BackColor = ex.IsCompleted ? UI.Orange : UI.Green;
        }

        void RefreshProgressBar()
        {
            if (selDay == null)
            {
                lblProgress.Text  = "Select a day to see progress";
                barProgress.Value = 0;
                return;
            }
            int total = selDay.Exercises.Count;
            int done  = selDay.CompletedCount;
            barProgress.Maximum = Math.Max(1, total);
            barProgress.Value   = Math.Min(done, barProgress.Maximum);
            lblProgress.Text    = total == 0
                ? "No exercises yet"
                : $"Completed: {done} / {total}  ({(total > 0 ? (int)((double)done / total * 100) : 0)}%)";
        }

        // ─────────────────────────────────────────────────────────────────────
        //  OWNER-DRAW — always clear full background first to avoid artifacts
        // ─────────────────────────────────────────────────────────────────────

        void DrawDay(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var day = (WorkoutDay)lstDays.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g   = e.Graphics;

            // 1. Clear entire bounds with solid background (no artifacts)
            using var bgBrush = new SolidBrush(sel ? Color.FromArgb(235, 251, 252) : UI.BgCard);
            g.FillRectangle(bgBrush, e.Bounds);

            // 2. Accent left bar
            using (var ab = new SolidBrush(sel ? UI.Cyan : Color.FromArgb(200, 230, 235)))
                g.FillRectangle(ab, e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height);

            // 3. Bottom separator
            using (var sp = new Pen(UI.Border))
                g.DrawLine(sp, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            int tx = e.Bounds.X + 14;
            using (var b = new SolidBrush(sel ? UI.Cyan : UI.TextFaded))
                g.DrawString(day.DayOfWeek.ToString().Substring(0, 3).ToUpper(),
                    new Font("Segoe UI", 7.5f, FontStyle.Bold), b, tx, e.Bounds.Y + 6);

            using (var b = new SolidBrush(UI.TextDark))
                g.DrawString(day.DayLabel,
                    new Font("Segoe UI", 9.5f, FontStyle.Bold), b, tx, e.Bounds.Y + 23);

            using (var b = new SolidBrush(UI.TextMid))
                g.DrawString($"{day.Exercises.Count} ex  •  {day.Difficulty}",
                    new Font("Segoe UI", 8f), b, tx, e.Bounds.Y + 41);

            // Progress mini-bar
            int total = day.Exercises.Count;
            if (total > 0)
            {
                int bw = 48, bh = 5;
                int bx = e.Bounds.Right - 62;
                int by = e.Bounds.Y + e.Bounds.Height / 2 - 3;
                using (var b = new SolidBrush(Color.FromArgb(220, 225, 235)))
                    g.FillRectangle(b, bx, by, bw, bh);
                int fill = (int)((double)day.CompletedCount / total * bw);
                if (fill > 0)
                    using (var b = new SolidBrush(UI.Cyan))
                        g.FillRectangle(b, bx, by, fill, bh);
                using (var b = new SolidBrush(UI.TextFaded))
                    g.DrawString($"{day.CompletedCount}/{total}",
                        new Font("Segoe UI", 7f), b, bx, by + 8);
            }
        }

        void DrawExercise(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var ex  = (Exercise)lstExercises.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g   = e.Graphics;

            // 1. Clear entire bounds
            using var bgBrush = new SolidBrush(
                ex.IsCompleted ? Color.FromArgb(235, 250, 240)
                : sel          ? Color.FromArgb(235, 249, 252)
                :                UI.BgCard);
            g.FillRectangle(bgBrush, e.Bounds);

            // 2. Accent left bar
            Color catColor = ex.Category == "Cardio"      ? UI.Orange
                           : ex.Category == "Flexibility" ? UI.Purple
                           :                                UI.Blue;
            using (var b = new SolidBrush(catColor))
                g.FillRectangle(b, e.Bounds.X, e.Bounds.Y, 4, e.Bounds.Height);

            // 3. Separator
            using (var sp = new Pen(UI.Border))
                g.DrawLine(sp, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            int tx = e.Bounds.X + 14;

            // Category tag pill
            var tagSize = g.MeasureString(ex.Category, new Font("Segoe UI", 7.5f, FontStyle.Bold));
            var tagRect = new Rectangle(tx, e.Bounds.Y + 8, (int)tagSize.Width + 10, 17);
            using (var b = new SolidBrush(Color.FromArgb(25, catColor.R, catColor.G, catColor.B)))
                g.FillRectangle(b, tagRect);
            using (var b = new SolidBrush(catColor))
                g.DrawString(ex.Category, new Font("Segoe UI", 7.5f, FontStyle.Bold), b, tagRect.X + 4, tagRect.Y + 2);

            // Name (strikethrough if completed)
            var nameStyle = ex.IsCompleted
                ? new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Strikeout)
                : new Font("Segoe UI", 10, FontStyle.Bold);
            using (var b = new SolidBrush(ex.IsCompleted ? UI.TextFaded : UI.TextDark))
                g.DrawString(ex.Name, nameStyle, b, tx, e.Bounds.Y + 30);

            // Summary
            using (var b = new SolidBrush(UI.TextMid))
                g.DrawString(ex.Summary, new Font("Segoe UI", 8f), b, tx, e.Bounds.Y + 50);

            // Checkbox on right
            int cbX = e.Bounds.Right - 32, cbY = e.Bounds.Y + e.Bounds.Height / 2 - 9;
            var cb  = new Rectangle(cbX, cbY, 18, 18);
            using (var p = new Pen(ex.IsCompleted ? UI.Green : Color.FromArgb(180, 190, 200), 1.5f))
                g.DrawRectangle(p, cb);
            if (ex.IsCompleted)
            {
                using (var b = new SolidBrush(Color.FromArgb(40, UI.Green.R, UI.Green.G, UI.Green.B)))
                    g.FillRectangle(b, cb);
                using (var b = new SolidBrush(UI.Green))
                    g.DrawString("✓", new Font("Segoe UI", 10, FontStyle.Bold), b, cbX, cbY);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EVENT HANDLERS
        // ─────────────────────────────────────────────────────────────────────

        void OnDaySelected(object? s, EventArgs e)
        {
            if (lstDays.SelectedItem == null) return;
            selDay = (WorkoutDay)lstDays.SelectedItem;
            selEx  = null;
            lstExercises.Items.Clear();
            foreach (var ex in selDay.Exercises) lstExercises.Items.Add(ex);
            SetDetailVisible(false);
            RefreshProgressBar();
        }

        void OnExerciseSelected(object? s, EventArgs e)
        {
            if (lstExercises.SelectedItem == null) return;
            selEx = (Exercise)lstExercises.SelectedItem;
            LoadDetail(selEx);
        }

        // Click right-side checkbox in list to toggle completion
        void OnExerciseListClick(object? s, MouseEventArgs e)
        {
            int idx = lstExercises.IndexFromPoint(e.Location);
            if (idx < 0 || e.X < lstExercises.Width - 40) return;
            var ex = (Exercise)lstExercises.Items[idx];
            ex.IsCompleted = !ex.IsCompleted;
            lstExercises.Invalidate();
            lstDays.Invalidate();
            RefreshProgressBar();
            AppState.NotifyStatsChanged();
            if (selEx == ex) { RefreshSummary(ex); RefreshToggleBtn(ex); }
        }

        void ToggleDone(object? s, EventArgs e)
        {
            if (selEx == null) return;
            selEx.IsCompleted = !selEx.IsCompleted;
            lstExercises.Invalidate();
            lstDays.Invalidate();
            RefreshSummary(selEx);
            RefreshToggleBtn(selEx);
            RefreshProgressBar();
            AppState.NotifyStatsChanged();
        }

        void OnAddDay(object? s, EventArgs e)
        {
            using var dlg = new AddDayDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null)
            {
                plan.Days.Add(dlg.Result);
                lstDays.Items.Add(dlg.Result);
                lstDays.SelectedItem = dlg.Result;
            }
        }

        void OnRemoveDay(object? s, EventArgs e)
        {
            if (selDay == null) return;
            if (MessageBox.Show($"Remove \"{selDay.DayLabel}\" and all its exercises?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                plan.Days.Remove(selDay);
                lstDays.Items.Remove(selDay);
                selDay = null; selEx = null;
                lstExercises.Items.Clear();
                SetDetailVisible(false);
            }
        }

        void OnAddExercise(object? s, EventArgs e)
        {
            if (selDay == null) { MessageBox.Show("Select a workout day first.", "No Day Selected", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            using var dlg = new AddExerciseDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null)
            {
                selDay.Exercises.Add(dlg.Result);
                lstExercises.Items.Add(dlg.Result);
                lstExercises.SelectedItem = dlg.Result;
                lstDays.Invalidate();
                RefreshProgressBar();
            }
        }

        void OnRemoveExercise(object? s, EventArgs e)
        {
            if (selEx == null || selDay == null) return;
            if (MessageBox.Show($"Delete \"{selEx.Name}\"?",
                    "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                selDay.Exercises.Remove(selEx);
                lstExercises.Items.Remove(selEx);
                selEx = null;
                SetDetailVisible(false);
                lstDays.Invalidate();
                RefreshProgressBar();
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ADD DAY DIALOG
    // ═════════════════════════════════════════════════════════════════════════
    public class AddDayDialog : Form
    {
        public WorkoutDay? Result { get; private set; }
        TextBox txtName, txtNotes;
        ComboBox cboDow, cboDiff;

        public AddDayDialog()
        {
            Text = "Add Workout Day"; ClientSize = new Size(430, 316);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = UI.BgDialog;

            L("Day Label:", 20, 20);
            txtName = T(20, 44, 390, "e.g. Day 1 — Upper Body");
            L("Day of Week:", 20, 90);
            cboDow = new ComboBox { Bounds = new Rectangle(20, 114, 175, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            foreach (DayOfWeek d in Enum.GetValues(typeof(DayOfWeek))) cboDow.Items.Add(d);
            cboDow.SelectedIndex = 1; Controls.Add(cboDow);
            L("Difficulty:", 214, 90);
            cboDiff = new ComboBox { Bounds = new Rectangle(214, 114, 196, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            cboDiff.Items.AddRange(new object[] { "Beginner", "Intermediate", "Advanced" });
            cboDiff.SelectedIndex = 0; Controls.Add(cboDiff);
            L("Notes (optional):", 20, 158);
            txtNotes = T(20, 182, 390, "e.g. Focus on upper body push", multiline: true, h: 58);

            var ok = B("Add Day", UI.Cyan, UI.TextDark, 256, 264, 154);
            ok.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Enter a day label.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
                Result = new WorkoutDay { DayLabel = txtName.Text.Trim(), DayOfWeek = (DayOfWeek)cboDow.SelectedItem!, Difficulty = cboDiff.SelectedItem?.ToString() ?? "Beginner", Notes = txtNotes.Text };
                DialogResult = DialogResult.OK;
            };
            var cancel = B("Cancel", UI.BgCancel, UI.TextMid, 20, 264, 120);
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }

        void L(string t, int x, int y) => Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid });
        TextBox T(int x, int y, int w, string ph, bool multiline = false, int h = 28) { var tb = new TextBox { Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, Multiline = multiline, PlaceholderText = ph }; Controls.Add(tb); return tb; }
        Button B(string t, Color bg, Color fg, int x, int y, int w) { var b = new Button { Text = t, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(x, y, w, 38), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b; }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ADD EXERCISE DIALOG
    // ═════════════════════════════════════════════════════════════════════════
    public class AddExerciseDialog : Form
    {
        public Exercise? Result { get; private set; }
        TextBox txtName, txtSets, txtReps, txtWeight, txtDuration, txtNotes;
        ComboBox cboCat;
        CheckBox chkCardio;
        Panel panStrength, panCardio;

        public AddExerciseDialog()
        {
            Text = "Add Exercise"; ClientSize = new Size(450, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = UI.BgDialog;

            L("Exercise Name:", 20, 20); txtName = T(20, 44, 410, "e.g. Bench Press");
            L("Category:", 20, 90);
            cboCat = new ComboBox { Bounds = new Rectangle(20, 114, 175, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            cboCat.Items.AddRange(new object[] { "Strength", "Cardio", "Flexibility" });
            cboCat.SelectedIndex = 0; Controls.Add(cboCat);
            chkCardio = new CheckBox { Text = "Cardio / Duration mode", AutoSize = true, Location = new Point(216, 118), Font = new Font("Segoe UI", 9.5f), ForeColor = UI.TextMid };
            chkCardio.CheckedChanged += (s, e) => { panStrength.Visible = !chkCardio.Checked; panCardio.Visible = chkCardio.Checked; };
            Controls.Add(chkCardio);
            L("Sets:", 20, 158); L("Reps:", 130, 158); L("Weight (kg):", 240, 158);
            panStrength = new Panel { Bounds = new Rectangle(20, 180, 410, 34), BackColor = UI.BgDialog };
            txtSets   = IT(panStrength,   0, 0, 90,  "3");
            txtReps   = IT(panStrength, 110, 0, 90,  "12");
            txtWeight = IT(panStrength, 220, 0, 100, "0");
            Controls.Add(panStrength);
            L("Duration (minutes):", 20, 158);
            panCardio = new Panel { Bounds = new Rectangle(20, 180, 200, 34), BackColor = UI.BgDialog, Visible = false };
            txtDuration = IT(panCardio, 0, 0, 140, "20");
            Controls.Add(panCardio);
            L("Notes (optional):", 20, 228); txtNotes = T(20, 252, 410, "e.g. Use controlled tempo...", multiline: true, h: 72);

            var ok = B("Add Exercise", UI.Cyan, UI.TextDark, 278, 366, 152);
            ok.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtName.Text)) { MessageBox.Show("Enter an exercise name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
                int.TryParse(txtSets.Text, out int sets); int.TryParse(txtReps.Text, out int reps); double.TryParse(txtWeight.Text, out double wt); int.TryParse(txtDuration.Text, out int dur);
                Result = new Exercise { Name = txtName.Text.Trim(), Category = cboCat.SelectedItem?.ToString() ?? "Strength", Sets = sets, Reps = reps, WeightKg = wt, DurationMinutes = chkCardio.Checked ? dur : 0, Notes = txtNotes.Text };
                DialogResult = DialogResult.OK;
            };
            var cancel = B("Cancel", UI.BgCancel, UI.TextMid, 20, 366, 126);
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }

        void L(string t, int x, int y) => Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid });
        TextBox T(int x, int y, int w, string ph, bool multiline = false, int h = 28) { var tb = new TextBox { Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, Multiline = multiline, PlaceholderText = ph }; Controls.Add(tb); return tb; }
        TextBox IT(Panel p, int x, int y, int w, string def) { var tb = new TextBox { Bounds = new Rectangle(x, y, w, 28), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, Text = def }; p.Controls.Add(tb); return tb; }
        Button B(string t, Color bg, Color fg, int x, int y, int w) { var b = new Button { Text = t, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(x, y, w, 40), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b; }
    }
}
