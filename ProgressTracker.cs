// ─────────────────────────────────────────────────────────────────────────────
// ProgressTracker.cs  —  NEW FILE
// Pure-logic classes: ProgressHistory, DailyLog, BmiCalculator.
// No UI code here — keeps logic separated from UI as requested.
// ─────────────────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;

namespace FitPlannerApp
{
    // ─────────────────────────────────────────────────────────────────────────
    // DailyLog  —  one entry per day stored in ProgressHistory
    // ─────────────────────────────────────────────────────────────────────────
    public class DailyLog
    {
        public DateTime Date                 { get; set; } = DateTime.Today;
        public double   WeightKg             { get; set; }
        public int      ExercisesCompleted   { get; set; }
        public double   CaloriesConsumed     { get; set; }
        public double   ProteinConsumed      { get; set; }
        public string   Notes                { get; set; } = "";

        public override string ToString() => Date.ToString("ddd, MMM d");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ProgressHistory  —  in-memory list of DailyLogs + weekly summary helpers
    // ─────────────────────────────────────────────────────────────────────────
    public class ProgressHistory
    {
        public List<DailyLog> Logs { get; } = new();

        /// <summary>
        /// Save or update today's snapshot.  Call from "Save Today" button.
        /// </summary>
        public void SaveToday(double weightKg, string notes = "")
        {
            // Remove existing entry for today if present
            Logs.RemoveAll(l => l.Date.Date == DateTime.Today.Date);

            Logs.Add(new DailyLog
            {
                Date               = DateTime.Today,
                WeightKg           = weightKg,
                ExercisesCompleted = AppState.TodayExercisesCompleted,
                CaloriesConsumed   = AppState.TodayCaloriesConsumed,
                ProteinConsumed    = AppState.TodayProteinConsumed,
                Notes              = notes
            });

            // Keep sorted newest-first
            Logs.Sort((a, b) => b.Date.CompareTo(a.Date));
        }

        /// <summary>Returns logs for the last 7 calendar days, newest first.</summary>
        public List<DailyLog> LastSevenDays()
        {
            var cutoff = DateTime.Today.AddDays(-6);
            return Logs.FindAll(l => l.Date.Date >= cutoff.Date);
        }

        // ── Weekly summary helpers ─────────────────────────────────────────
        public double AverageWeight()
        {
            var week = LastSevenDays();
            if (week.Count == 0) return 0;
            double sum = 0; foreach (var l in week) sum += l.WeightKg; return sum / week.Count;
        }

        public int TotalExercisesThisWeek()
        {
            int sum = 0; foreach (var l in LastSevenDays()) sum += l.ExercisesCompleted; return sum;
        }

        public double AverageCaloriesThisWeek()
        {
            var week = LastSevenDays();
            if (week.Count == 0) return 0;
            double sum = 0; foreach (var l in week) sum += l.CaloriesConsumed; return sum / week.Count;
        }

        /// <summary>
        /// Simple weight trend: positive = gaining, negative = losing (kg over the week).
        /// </summary>
        public double WeightTrend()
        {
            var week = LastSevenDays();
            if (week.Count < 2) return 0;
            // week[0] = newest, week[last] = oldest
            return week[0].WeightKg - week[week.Count - 1].WeightKg;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BmiCalculator  —  pure logic, no UI
    // ─────────────────────────────────────────────────────────────────────────
    public static class BmiCalculator
    {
        public static double Calculate(double weightKg, double heightCm)
        {
            if (heightCm <= 0) return 0;
            return weightKg / Math.Pow(heightCm / 100.0, 2);
        }

        public static string Category(double bmi) =>
            bmi < 18.5 ? "Underweight"
            : bmi < 25 ? "Normal Weight"
            : bmi < 30 ? "Overweight"
            :             "Obese";

        /// <summary>
        /// Mifflin-St Jeor BMR → multiply by 1.55 (moderate activity) for TDEE.
        /// isMale: true = male, false = female.
        /// </summary>
        public static double RecommendedCalories(double weightKg, double heightCm, int age, bool isMale)
        {
            double bmr = isMale
                ? 10 * weightKg + 6.25 * heightCm - 5 * age + 5
                : 10 * weightKg + 6.25 * heightCm - 5 * age - 161;
            return Math.Round(bmr * 1.55);   // Moderate activity
        }

        /// <summary>
        /// Recommended protein: 1.6 g per kg of body weight (standard for muscle maintenance).
        /// </summary>
        public static double RecommendedProtein(double weightKg) =>
            Math.Round(weightKg * 1.6, 1);

        /// <summary>Human-readable advice line based on BMI.</summary>
        public static string Advice(double bmi) =>
            bmi < 18.5 ? "Consider a calorie surplus and strength training to build muscle."
            : bmi < 25 ? "Great! Maintain your current routine and stay consistent."
            : bmi < 30 ? "A moderate calorie deficit (~300-500 kcal/day) and cardio can help."
            :             "Please consult a healthcare professional for a personalised plan.";

        public static System.Drawing.Color CategoryColor(double bmi)
        {
            if (bmi < 18.5) return UI.Gold;
            if (bmi < 25)   return UI.Green;
            if (bmi < 30)   return UI.Orange;
            return UI.Red;
        }
    }
}
