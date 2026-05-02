using System;

namespace FitPlannerApp
{
    public static class AppState
    {
        // ── Live plan references (set once by MainForm via SeedData.InitAppState) ──
        public static WorkoutPlan?  CurrentWorkout { get; set; }
        public static DietPlan?     CurrentDiet    { get; set; }

        // ── Progress history ──────────────────────────────────────────────────
        public static ProgressHistory History { get; } = new ProgressHistory();

        // =====================================================================
        //  REQUIREMENT 1 — Explicit shared nutrition values
        //  Written by DietPlannerPanel.RefreshMacros() on every checkbox toggle.
        //  Read by DashboardPanel.RefreshDashboard().
        // =====================================================================

        /// <summary>Total calories from food items the user has ticked today.</summary>
        public static double CaloriesConsumed  { get; private set; }

        /// <summary>Total protein (g) from ticked food items.</summary>
        public static double ProteinConsumed   { get; private set; }

        /// <summary>Total carbohydrates (g) from ticked food items.</summary>
        public static double CarbsConsumed     { get; private set; }

        /// <summary>Total fat (g) from ticked food items.</summary>
        public static double FatConsumed       { get; private set; }

        /// <summary>
        /// The daily calorie target (read from the active DietPlan).
        /// Exposed here so DashboardPanel doesn't need a direct DietPlan reference.
        /// </summary>
        public static double DailyCalorieGoal  => CurrentDiet?.DailyCalorieGoal ?? 2500;

        /// <summary>Calories not yet consumed (never negative).</summary>
        public static double CaloriesRemaining => Math.Max(0, DailyCalorieGoal - CaloriesConsumed);

        // =====================================================================
        //  REQUIREMENT 2 — Write method called by DietPlannerPanel
        // =====================================================================

        /// <summary>
        /// Called by DietPlannerPanel.RefreshMacros() after every checkbox toggle
        /// or whenever the selected day changes.  Stores the new consumed totals
        /// and fires StatsChanged so the Dashboard refreshes immediately.
        /// </summary>
        public static void UpdateNutrition(double calories, double protein,
                                           double carbs,    double fat)
        {
            CaloriesConsumed = calories;
            ProteinConsumed  = protein;
            CarbsConsumed    = carbs;
            FatConsumed      = fat;
            NotifyStatsChanged();   // triggers DashboardPanel.RefreshDashboard()
        }

        // ── Workout derived stats (unchanged) ─────────────────────────────────

        /// <summary>Number of exercises marked IsCompleted across all workout days.</summary>
        public static int TodayExercisesCompleted
        {
            get
            {
                if (CurrentWorkout == null) return 0;
                int n = 0;
                foreach (var day in CurrentWorkout.Days)
                    foreach (var ex in day.Exercises)
                        if (ex.IsCompleted) n++;
                return n;
            }
        }

        /// <summary>Total exercises across all workout days.</summary>
        public static int TotalExercises
        {
            get
            {
                if (CurrentWorkout == null) return 0;
                int n = 0;
                foreach (var day in CurrentWorkout.Days) n += day.Exercises.Count;
                return n;
            }
        }

        // ── Legacy computed helpers (kept for backward compatibility) ─────────
        // These are used by some older code paths; they now delegate to the
        // explicit properties so everything stays in sync.

        public static double TodayCaloriesConsumed => CaloriesConsumed;
        public static double TodayProteinConsumed  => ProteinConsumed;
        public static double TodayCarbsConsumed    => CarbsConsumed;
        public static double TodayFatConsumed      => FatConsumed;

        // ── Change notification ────────────────────────────────────────────────

        /// <summary>
        /// Raised whenever nutrition or workout data changes.
        /// DashboardPanel subscribes to this and calls RefreshDashboard().
        /// </summary>
        public static event Action? StatsChanged;

        /// <summary>
        /// Fires StatsChanged.  Call this after any data mutation that the
        /// Dashboard should reflect.  DietPlannerPanel calls it via UpdateNutrition().
        /// WorkoutPlannerPanel calls it directly when exercise completion changes.
        /// </summary>
        public static void NotifyStatsChanged() => StatsChanged?.Invoke();
    }
}
