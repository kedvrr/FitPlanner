using System;
using System.Drawing;
using System.Windows.Forms;

namespace FitPlannerApp
{
    public class DietPlannerPanel : Panel
    {
        DietPlan     plan;
        DayMealPlan? selDay;
        Meal?        selMeal;

        FlowLayoutPanel calRow;
        ListBox         lstMeals, lstFoods;
        Label           lblFoodHint;   // "Select a meal then click + Add Food"

        // ── MacroRing controls (replaces old label+bar pairs) ────────────────
        MacroRing ringCal, ringPro, ringCrb, ringFat;

        public DietPlannerPanel(DietPlan diet)
        {
            plan = diet;
            BackColor = UI.BgPage;
            Build();
        }

        // ─────────────────────────────────────────────────────────────────────
        void Build()
        {
            // ── Page header  (taller so title + subtitle never get clipped) ──
            var topBar = new Panel { Dock = DockStyle.Top, Height = 96, BackColor = UI.BgPage };
            var lblTitle = UI.MakeLabel("Meal Planner", 22, FontStyle.Bold, UI.TextDark);
            lblTitle.Location = new Point(24, 14);
            var lblSub = UI.MakeLabel(
                $"Plan: {plan.PlanName}   •   Daily goal: {plan.DailyCalorieGoal:N0} kcal",
                9.5f, FontStyle.Regular, UI.TextMid);
            lblSub.Location = new Point(26, 52);
            var btnAdd = UI.MakeBtn("+ Add Meal", UI.Orange, Color.White, 138, 38);
            btnAdd.Anchor   = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.Location = new Point(topBar.Width - 156, 28);
            topBar.SizeChanged += (s, e) => btnAdd.Location = new Point(topBar.Width - 156, 28);
            btnAdd.Click += OnAddMeal;
            // Bottom divider
            var topSep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = UI.Border };
            topBar.Controls.AddRange(new Control[] { lblTitle, lblSub, btnAdd, topSep });
            Controls.Add(topBar);

            // ── Calendar strip ────────────────────────────────────────────────
            var calWrap = new Panel { Dock = DockStyle.Top, Height = 86, BackColor = UI.BgPage, Padding = new Padding(14, 8, 14, 8) };
            calRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, BackColor = UI.BgPage,
                FlowDirection = FlowDirection.LeftToRight, WrapContents = false
            };
            foreach (var day in plan.Week) calRow.Controls.Add(MakeDayBtn(day));
            calWrap.Controls.Add(calRow);
            Controls.Add(calWrap);

            // ── Three-column outer (outer TLP just for column widths) ─────────
            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = UI.BgPage,
                Padding = new Padding(14, 200, 14, 14),    // 10px top gap
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34f));
            Controls.Add(outer);

            outer.Controls.Add(BuildMealsColumn(),  0, 0);
            outer.Controls.Add(BuildFoodsColumn(),  1, 0);
            outer.Controls.Add(BuildMacrosColumn(), 2, 0);

            // Pre-select today
            if (plan.Week.Count > 0)
            {
                SelectDay(plan.Week[0]);
                if (calRow.Controls.Count > 0) HighlightDayBtn((Button)calRow.Controls[0]);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  COLUMN BUILDERS
        //  Rule: col has exactly 2 children:
        //    1. topBar  (Dock=Top, fixed Height) — header + sep + buttons + sep
        //    2. list    (Dock=Fill)
        //  Inside topBar every child uses Dock=Top with explicit Height.
        //  No absolute Bounds, no Anchor, no Width=2000 tricks.
        //  This is immune to timing, sizing, and z-order issues.
        // ─────────────────────────────────────────────────────────────────────

        // Build the topBar panel. All children are Dock=Top in add-order.
        Panel BuildTopBar(string title, Color accent,
                          Action<Panel> addButtons, bool addBtnRow = true)
        {
            int totalH = addBtnRow ? 98 : 47;   // 46+1+50+1 = 98  OR  46+1 = 47

            var top = new Panel { Dock = DockStyle.Top, Height = totalH, BackColor = UI.BgCard };

            // ── Header  (Dock=Top, 46 px) ─────────────────────────────────
            var hdr = new Panel { Dock = DockStyle.Top, Height = 46, BackColor = UI.BgHeader };
            hdr.Controls.Add(new Panel { BackColor = accent, Width = 4, Dock = DockStyle.Left });
            var lbl = UI.MakeLabel(title, 10.5f, FontStyle.Bold, UI.TextDark);
            lbl.Location = new Point(16, 13);
            hdr.Controls.Add(lbl);

            // ── Separator  (Dock=Top, 1 px) ───────────────────────────────
            var sep1 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = UI.Border };

            top.Controls.Add(hdr);   // rendered topmost because added first
            top.Controls.Add(sep1);

            if (addBtnRow)
            {
                // ── Button row  (Dock=Top, 50 px) ─────────────────────────
                var btnRow = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = UI.BgCard };
                addButtons(btnRow);

                // ── Separator  (Dock=Top, 1 px) ───────────────────────────
                var sep2 = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = UI.Border };

                top.Controls.Add(btnRow);
                top.Controls.Add(sep2);
            }

            return top;
        }

        // Add a button to a button-row panel
        void AddBtnToRow(Panel row, ref int x, string text,
                         Color bg, Color fg, int w, EventHandler click)
        {
            var b = UI.MakeBtn(text, bg, fg, w, 32, 8.5f);
            b.Location = new Point(x, 9);
            b.Click   += click;
            row.Controls.Add(b);
            x += w + 8;
        }

        // ── COLUMN 1: Meals ───────────────────────────────────────────────────
        Panel BuildMealsColumn()
        {
            var col = new Panel
            {
                BackColor = UI.BgCard,
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 0, 5, 0)
            };

            var top = BuildTopBar("Meals", UI.Orange, btnRow =>
            {
                int x = 8;
                AddBtnToRow(btnRow, ref x, "+ Add Meal", UI.Orange, Color.White, 112, OnAddMeal);
                AddBtnToRow(btnRow, ref x, "Remove", UI.Red, Color.White, 84, OnRemoveMeal);
            });

            top.Dock = DockStyle.Top;

            lstMeals = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = UI.BgCard,
                ItemHeight = 70,
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false
            };

            lstMeals.DrawItem += DrawMeal;
            lstMeals.SelectedIndexChanged += OnMealSelected;

            // IMPORTANT ORDER
            col.Controls.Add(lstMeals);
            col.Controls.Add(top);

            return col;
        }

        // ── COLUMN 2: Food Items ──────────────────────────────────────────────
        Panel BuildFoodsColumn()
        {
            var col = new Panel
            {
                BackColor = UI.BgCard,
                Dock = DockStyle.Fill,
                Margin = new Padding(5, 0, 5, 0)
            };

            var top = BuildTopBar("Food Items  (tick = consumed)", UI.Blue, btnRow =>
            {
                int x = 8;
                AddBtnToRow(btnRow, ref x, "+ Add Food", UI.Cyan, UI.TextDark, 110, OnAddFood);
                AddBtnToRow(btnRow, ref x, "Remove", UI.Red, Color.White, 84, OnRemoveFood);
            });

            top.Dock = DockStyle.Top;

            lblFoodHint = UI.MakeLabel(
                "Select a meal on the left,\nthen click + Add Food.",
                9f, FontStyle.Regular, UI.TextMid, false);

            lblFoodHint.Location = new Point(14, 14);
            lblFoodHint.Size = new Size(280, 36);
            lblFoodHint.Visible = true;

            lstFoods = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                BackColor = UI.BgCard,
                ItemHeight = 64,
                DrawMode = DrawMode.OwnerDrawFixed,
                IntegralHeight = false,
                Visible = false
            };

            lstFoods.DrawItem += DrawFood;
            lstFoods.MouseClick += OnFoodClick;

            var listArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UI.BgCard
            };

            listArea.Controls.Add(lstFoods);
            listArea.Controls.Add(lblFoodHint);

            listArea.SizeChanged += (s, e) =>
            {
                lstFoods.Bounds = new Rectangle(0, 0, listArea.Width, listArea.Height);
                lblFoodHint.Width = Math.Max(10, listArea.Width - 28);
            };

            // IMPORTANT ORDER
            col.Controls.Add(listArea);
            col.Controls.Add(top);

            return col;
        }

        // ── COLUMN 3: Macros ──────────────────────────────────────────────────
        Panel BuildMacrosColumn()
        {
            var col = new Panel
            {
                BackColor = UI.BgCard,
                Dock      = DockStyle.Fill,
                Margin    = new Padding(5, 0, 0, 0)
            };

            var top = BuildTopBar("Macros  (Consumed / Planned)", UI.Green,
                                  _ => { }, addBtnRow: false);

            // ── 2×2 grid of MacroRing controls ───────────────────────────────
            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 2,
                BackColor   = UI.BgCard,
                Padding     = new Padding(8)
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
            grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

            ringCal = new MacroRing("Calories", UI.Orange, "kcal") { Dock = DockStyle.Fill };
            ringPro = new MacroRing("Protein",  UI.Green,  "g")    { Dock = DockStyle.Fill };
            ringCrb = new MacroRing("Carbs",    UI.Gold,   "g")    { Dock = DockStyle.Fill };
            ringFat = new MacroRing("Fat",       UI.Blue,   "g")    { Dock = DockStyle.Fill };

            grid.Controls.Add(ringCal, 0, 0);
            grid.Controls.Add(ringPro, 1, 0);
            grid.Controls.Add(ringCrb, 0, 1);
            grid.Controls.Add(ringFat, 1, 1);

            col.Controls.Add(top);    // DockStyle.Top
            col.Controls.Add(grid);   // DockStyle.Fill
            return col;
        }

        // ── Calendar ──────────────────────────────────────────────────────────
        Button MakeDayBtn(DayMealPlan day)
        {
            bool today = day.Date.Date == DateTime.Today.Date;
            var btn = new Button
            {
                Width = 88, Height = 66, FlatStyle = FlatStyle.Flat,
                BackColor = today ? UI.Orange : UI.BgCard,
                ForeColor = today ? Color.White : UI.TextDark,
                Tag = day, Cursor = Cursors.Hand, Margin = new Padding(3, 0, 3, 0),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Text = day.Date.ToString("ddd") + "\n" + day.Date.Day,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderColor = today ? UI.Orange : UI.Border;
            btn.FlatAppearance.BorderSize  = 1;
            btn.Click += (s, e) => { SelectDay(day); HighlightDayBtn((Button)s); };
            return btn;
        }

        void HighlightDayBtn(Button active)
        {
            foreach (Control c in calRow.Controls)
                if (c is Button b)
                { b.BackColor = UI.BgCard; b.ForeColor = UI.TextDark; b.FlatAppearance.BorderColor = UI.Border; }
            active.BackColor = UI.Orange;
            active.ForeColor = Color.White;
            active.FlatAppearance.BorderColor = UI.Orange;
        }

        void SelectDay(DayMealPlan day)
        {
            selDay  = day;
            selMeal = null;
            lstMeals.BeginUpdate();
            lstMeals.Items.Clear();
            foreach (var m in day.Meals) lstMeals.Items.Add(m);
            lstMeals.EndUpdate();
            // Force scroll to index 0 so Breakfast is always the first visible item
            if (lstMeals.Items.Count > 0) lstMeals.TopIndex = 0;
            lstFoods.Items.Clear();
            lblFoodHint.Visible = true;
            lstFoods.Visible    = false;
            RefreshMacros();
        }

        // ── Refresh macros — planned = all items, consumed = ticked items ─────
        // REQUIREMENT 2: After calculating consumed totals, write them into AppState
        // so DashboardPanel.RefreshDashboard() gets accurate values.
        void RefreshMacros()
        {
            if (selDay == null) return;

            double calP = 0, proP = 0, crbP = 0, fatP = 0;  // planned (all foods)
            double calC = 0, proC = 0, crbC = 0, fatC = 0;  // consumed (ticked only)

            foreach (var m in selDay.Meals)
                foreach (var f in m.Foods)
                {
                    calP += f.Calories;  proP += f.ProteinG;  crbP += f.CarbsG;  fatP += f.FatG;
                    if (f.IsConsumed)
                    { calC += f.Calories; proC += f.ProteinG; crbC += f.CarbsG; fatC += f.FatG; }
                }

            // ── Update MacroRings with animated fill ──────────────────────────
            ringCal.Update(calC, calP, plan.DailyCalorieGoal);
            ringPro.Update(proC, proP, plan.ProteinGoalG);
            ringCrb.Update(crbC, crbP, plan.CarbsGoalG);
            ringFat.Update(fatC, fatP, plan.FatGoalG);

            // ── REQUIREMENT 2: Push consumed values into AppState ─────────────
            // AppState.UpdateNutrition() stores the values AND fires StatsChanged,
            // which triggers DashboardPanel.RefreshDashboard() automatically.
            AppState.UpdateNutrition(calC, proC, crbC, fatC);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  OWNER DRAW — always fill full row background first
        // ─────────────────────────────────────────────────────────────────────

        void DrawMeal(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var meal = (Meal)lstMeals.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g    = e.Graphics;

            // 1. ALWAYS clear full row background
            Color bg = sel ? Color.FromArgb(253, 244, 238) : UI.BgCard;
            g.FillRectangle(new SolidBrush(bg), e.Bounds);

            // 2. Meal-type color
            Color c = meal.Type == MealType.Breakfast ? UI.Gold
                    : meal.Type == MealType.Lunch     ? UI.Green
                    : meal.Type == MealType.Dinner    ? UI.Blue
                    : UI.Purple;

            // 3. Left accent bar (solid, 5px wide, full height)
            g.FillRectangle(new SolidBrush(c), e.Bounds.X, e.Bounds.Y, 5, e.Bounds.Height);

            // 4. Bottom separator
            g.DrawLine(new Pen(UI.Border), e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            int tx = e.Bounds.X + 14;

            // Meal type label (small, colored)
            g.DrawString(meal.Type.ToString(),
                new Font("Segoe UI", 7.5f, FontStyle.Bold),
                new SolidBrush(c), tx, e.Bounds.Y + 8);

            // Meal display name (bold)
            g.DrawString(meal.ToString(),
                new Font("Segoe UI", 10.5f, FontStyle.Bold),
                new SolidBrush(UI.TextDark), tx, e.Bounds.Y + 25);

            // Stats line: items count + planned kcal + consumed if any
            double allCal = 0, conCal = 0;
            foreach (var f in meal.Foods) { allCal += f.Calories; if (f.IsConsumed) conCal += f.Calories; }
            string statsLine = conCal > 0
                ? $"{meal.Foods.Count} items  •  {allCal:N0} kcal planned  •  {conCal:N0} kcal consumed"
                : $"{meal.Foods.Count} items  •  {allCal:N0} kcal";
            g.DrawString(statsLine,
                new Font("Segoe UI", 8f),
                new SolidBrush(UI.TextMid), tx, e.Bounds.Y + 49);
        }

        void DrawFood(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var food = (FoodItem)lstFoods.Items[e.Index];
            bool sel = (e.State & DrawItemState.Selected) != 0;
            var g    = e.Graphics;

            // 1. ALWAYS clear full row background
            Color bg = food.IsConsumed ? Color.FromArgb(234, 252, 241)
                     : sel             ? Color.FromArgb(234, 249, 253)
                     :                   UI.BgCard;
            g.FillRectangle(new SolidBrush(bg), e.Bounds);

            // 2. Bottom separator
            g.DrawLine(new Pen(UI.Border), e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            int tx = e.Bounds.X + 12;

            // Calorie badge (top-right, always visible)
            string calBadge = $"{food.Calories:N0} kcal";
            var badgeFont   = new Font("Segoe UI", 9f, FontStyle.Bold);
            var badgeSize   = e.Graphics.MeasureString(calBadge, badgeFont);
            float badgeX    = e.Bounds.Right - 46 - badgeSize.Width;
            g.DrawString(calBadge, badgeFont, new SolidBrush(UI.Orange), badgeX, e.Bounds.Y + 9);

            // Food name (bold, strikethrough if consumed)
            var nameFont = food.IsConsumed
                ? new Font("Segoe UI", 10, FontStyle.Bold | FontStyle.Strikeout)
                : new Font("Segoe UI", 10, FontStyle.Bold);
            g.DrawString(food.Name, nameFont,
                new SolidBrush(food.IsConsumed ? UI.Green : UI.TextDark),
                tx, e.Bounds.Y + 8);

            // Macro detail line
            g.DrawString(
                $"P:{food.ProteinG}g  C:{food.CarbsG}g  F:{food.FatG}g  ({food.ServingGrams}g serving)",
                new Font("Segoe UI", 8f), new SolidBrush(UI.TextMid),
                tx, e.Bounds.Y + 36);

            // Checkbox on right side
            int cbX = e.Bounds.Right - 30, cbY = e.Bounds.Y + e.Bounds.Height / 2 - 9;
            var cb  = new Rectangle(cbX, cbY, 18, 18);
            g.DrawRectangle(new Pen(food.IsConsumed ? UI.Green : Color.FromArgb(175, 185, 200), 1.5f), cb);
            if (food.IsConsumed)
            {
                g.FillRectangle(new SolidBrush(Color.FromArgb(55, UI.Green.R, UI.Green.G, UI.Green.B)), cb);
                g.DrawString("✓", new Font("Segoe UI", 10, FontStyle.Bold),
                    new SolidBrush(UI.Green), cbX, cbY);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  EVENTS
        // ─────────────────────────────────────────────────────────────────────

        void OnMealSelected(object? s, EventArgs e)
        {
            if (lstMeals.SelectedItem == null) return;
            selMeal = (Meal)lstMeals.SelectedItem;
            lstFoods.Items.Clear();
            foreach (var f in selMeal.Foods) lstFoods.Items.Add(f);
            if (lstFoods.Items.Count > 0) lstFoods.TopIndex = 0;
            // Show list, hide hint
            lblFoodHint.Visible = false;
            lstFoods.Visible    = true;
        }

        // Click right-side checkbox to toggle IsConsumed
        void OnFoodClick(object? s, MouseEventArgs e)
        {
            int idx = lstFoods.IndexFromPoint(e.Location);
            if (idx < 0 || e.X < lstFoods.Width - 42) return;
            var food = (FoodItem)lstFoods.Items[idx];
            food.IsConsumed = !food.IsConsumed;
            lstFoods.Invalidate();
            lstMeals.Invalidate();
            RefreshMacros();
        }

        void OnAddMeal(object? s, EventArgs e)
        {
            if (selDay == null)
            {
                MessageBox.Show("Select a day in the calendar first.", "No Day Selected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new AddMealDialog();
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null)
            {
                selDay.Meals.Add(dlg.Result);
                lstMeals.Items.Add(dlg.Result);
                lstMeals.SelectedItem = dlg.Result;
                RefreshMacros();
            }
        }

        void OnRemoveMeal(object? s, EventArgs e)
        {
            if (selMeal == null || selDay == null) return;
            if (MessageBox.Show($"Remove meal \"{selMeal}\"?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                selDay.Meals.Remove(selMeal);
                lstMeals.Items.Remove(selMeal);
                selMeal = null;
                lstFoods.Items.Clear();
                lblFoodHint.Visible = true;
                lstFoods.Visible    = false;
                RefreshMacros();
            }
        }

        void OnAddFood(object? s, EventArgs e)
        {
            if (selMeal == null)
            {
                MessageBox.Show(
                    "Please select a meal from the left list first,\nthen click + Add Food to add a food item.",
                    "No Meal Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using var dlg = new AddFoodDialog(selMeal.ToString());
            if (dlg.ShowDialog() == DialogResult.OK && dlg.Result != null)
            {
                selMeal.Foods.Add(dlg.Result);
                lstFoods.Items.Add(dlg.Result);
                lstFoods.Visible    = true;
                lblFoodHint.Visible = false;
                lstMeals.Invalidate();
                RefreshMacros();
            }
        }

        void OnRemoveFood(object? s, EventArgs e)
        {
            if (lstFoods.SelectedItem == null || selMeal == null) return;
            var food = (FoodItem)lstFoods.SelectedItem;
            if (MessageBox.Show($"Remove \"{food.Name}\"?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                selMeal.Foods.Remove(food);
                lstFoods.Items.Remove(food);
                if (lstFoods.Items.Count == 0)
                {
                    lblFoodHint.Visible = true;
                    lstFoods.Visible    = false;
                }
                lstMeals.Invalidate();
                RefreshMacros();
            }
        }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ADD MEAL DIALOG
    // ═════════════════════════════════════════════════════════════════════════
    public class AddMealDialog : Form
    {
        public Meal? Result { get; private set; }
        ComboBox cboType;
        TextBox txtName, txtNotes;

        public AddMealDialog()
        {
            Text = "Add Meal"; ClientSize = new Size(420, 306);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = UI.BgDialog;

            L("Meal Type:", 20, 20);
            cboType = new ComboBox { Bounds = new Rectangle(20, 44, 180, 28), DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 10) };
            foreach (MealType mt in Enum.GetValues(typeof(MealType))) cboType.Items.Add(mt);
            cboType.SelectedIndex = 0; Controls.Add(cboType);

            L("Custom Name (optional):", 20, 90);
            txtName = T(20, 114, 380, "e.g. Post-workout shake or leave blank");

            L("Notes:", 20, 154);
            txtNotes = T(20, 178, 380, "Any notes...", multiline: true, h: 50);

            var ok = B("Add Meal", UI.Orange, Color.White, 250, 250, 150);
            ok.Click += (s, e) => {
                Result = new Meal { Type = (MealType)cboType.SelectedItem!, CustomName = txtName.Text.Trim(), Notes = txtNotes.Text };
                DialogResult = DialogResult.OK;
            };
            var cancel = B("Cancel", UI.BgCancel, UI.TextMid, 20, 250, 120);
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }
        void L(string t, int x, int y) => Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid });
        TextBox T(int x, int y, int w, string ph, bool multiline = false, int h = 28) { var tb = new TextBox { Bounds = new Rectangle(x, y, w, h), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, Multiline = multiline, PlaceholderText = ph }; Controls.Add(tb); return tb; }
        Button B(string t, Color bg, Color fg, int x, int y, int w) { var b = new Button { Text = t, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(x, y, w, 38), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b; }
    }

    // ═════════════════════════════════════════════════════════════════════════
    //  ADD FOOD DIALOG — calories field is large & prominent
    // ═════════════════════════════════════════════════════════════════════════
    public class AddFoodDialog : Form
    {
        public FoodItem? Result { get; private set; }
        TextBox txtName, txtCal, txtPro, txtCarb, txtFat, txtServing;

        public AddFoodDialog(string mealName)
        {
            Text = $"Add Food to: {mealName}";
            ClientSize = new Size(450, 354);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false; BackColor = UI.BgDialog;

            // Top instruction banner
            var banner = new Panel { BackColor = Color.FromArgb(230, 246, 255), Bounds = new Rectangle(0, 0, 450, 34) };
            banner.Controls.Add(new Label { Text = "  ℹ  Enter the food name and its nutritional values per serving.", AutoSize = true, Location = new Point(8, 9), Font = new Font("Segoe UI", 8.5f), ForeColor = UI.Blue });
            Controls.Add(banner);

            // Name
            L("Food Name:", 20, 46);
            txtName = T(20, 68, 410, "e.g. Grilled Chicken Breast (150g)");

            // Calories — BIG and prominent
            L("⚡  Calories (kcal):", 20, 108);
            txtCal = T(20, 130, 200, "0");
            txtCal.Font      = new Font("Segoe UI", 12, FontStyle.Bold);
            txtCal.BackColor = Color.FromArgb(255, 248, 232);
            txtCal.Height    = 32;

            var lCalHint = new Label { Text = "← Enter total kcal for this serving", AutoSize = true, Location = new Point(228, 138), Font = new Font("Segoe UI", 8f), ForeColor = UI.Orange };
            Controls.Add(lCalHint);

            // Macros row
            L("Protein (g):",  20, 176); txtPro  = T(20,  198, 100, "0");
            L("Carbs (g):",   134, 176); txtCarb = T(134, 198, 100, "0");
            L("Fat (g):",     248, 176); txtFat  = T(248, 198, 100, "0");

            L("Serving size (g):", 20, 238);
            txtServing = T(20, 260, 130, "100");

            var ok = B("Add Food", UI.Cyan, UI.TextDark, 288, 296, 142);
            ok.Click += (s, e) => {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                { MessageBox.Show("Please enter a food name.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning); DialogResult = DialogResult.None; return; }
                double.TryParse(txtCal.Text,     out double cal);
                double.TryParse(txtPro.Text,     out double pro);
                double.TryParse(txtCarb.Text,    out double crb);
                double.TryParse(txtFat.Text,     out double fat);
                double.TryParse(txtServing.Text, out double srv);
                Result = new FoodItem { Name = txtName.Text.Trim(), Calories = cal, ProteinG = pro, CarbsG = crb, FatG = fat, ServingGrams = srv > 0 ? srv : 100 };
                DialogResult = DialogResult.OK;
            };
            var cancel = B("Cancel", UI.BgCancel, UI.TextMid, 20, 296, 126);
            cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
            Controls.AddRange(new Control[] { ok, cancel });
            AcceptButton = ok; CancelButton = cancel;
        }
        void L(string t, int x, int y) => Controls.Add(new Label { Text = t, AutoSize = true, Location = new Point(x, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = UI.TextMid });
        TextBox T(int x, int y, int w, string ph) { var tb = new TextBox { Bounds = new Rectangle(x, y, w, 28), Font = new Font("Segoe UI", 10), BackColor = UI.BgInput, BorderStyle = BorderStyle.FixedSingle, PlaceholderText = ph }; Controls.Add(tb); return tb; }
        Button B(string t, Color bg, Color fg, int x, int y, int w) { var b = new Button { Text = t, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = fg, BackColor = bg, FlatStyle = FlatStyle.Flat, Bounds = new Rectangle(x, y, w, 40), Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; Controls.Add(b); return b; }
    }
}
