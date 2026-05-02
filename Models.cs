using System;
using System.Collections.Generic;

namespace FitPlannerApp
{
    public class Exercise
    {
        public string Name            { get; set; } = "";
        public string Category        { get; set; } = "Strength";
        public int    Sets            { get; set; }
        public int    Reps            { get; set; }
        public int    DurationMinutes { get; set; }
        public double WeightKg        { get; set; }
        public string Notes           { get; set; } = "";
        public bool   IsCompleted     { get; set; }

        public string Summary =>
            DurationMinutes > 0
                ? $"Duration: {DurationMinutes} min  [{Category}]"
                : $"{Sets} sets × {Reps} reps @ {WeightKg} kg  [{Category}]";

        public override string ToString() => Name;
    }

    public class WorkoutDay
    {
        public string        DayLabel   { get; set; } = "";
        public DayOfWeek     DayOfWeek  { get; set; }
        public string        Difficulty { get; set; } = "Beginner";
        public string        Notes      { get; set; } = "";
        public List<Exercise> Exercises { get; set; } = new();

        public int CompletedCount => Exercises.FindAll(e => e.IsCompleted).Count;
        public override string ToString() => DayLabel;
    }

    public class WorkoutPlan
    {
        public string          PlanName  { get; set; } = "";
        public DateTime        CreatedOn { get; set; } = DateTime.Today;
        public List<WorkoutDay> Days     { get; set; } = new();
        public override string ToString() => PlanName;
    }

    public enum MealType { Breakfast, Lunch, Dinner, Snack }

    public class FoodItem
    {
        public string Name         { get; set; } = "";
        public double Calories     { get; set; }
        public double ProteinG     { get; set; }
        public double CarbsG       { get; set; }
        public double FatG         { get; set; }
        public double ServingGrams { get; set; }

        // ── Feature: Meal Tracking (feature 2) ───────────────────────────────
        /// <summary>True when user ticks the checkbox in DietPlannerPanel.</summary>
        public bool IsConsumed { get; set; }

        public override string ToString() => $"{Name}  ({Calories} kcal)";
    }

    public class Meal
    {
        public MealType       Type       { get; set; }
        public string         CustomName { get; set; } = "";
        public string         Notes      { get; set; } = "";
        public List<FoodItem> Foods      { get; set; } = new();

        public double TotalCalories { get { double t = 0; foreach (var f in Foods) t += f.Calories;  return t; } }
        public double TotalProtein  { get { double t = 0; foreach (var f in Foods) t += f.ProteinG;  return t; } }
        public double TotalCarbs    { get { double t = 0; foreach (var f in Foods) t += f.CarbsG;    return t; } }
        public double TotalFat      { get { double t = 0; foreach (var f in Foods) t += f.FatG;      return t; } }

        public override string ToString() =>
            string.IsNullOrWhiteSpace(CustomName) ? Type.ToString() : $"{Type} — {CustomName}";
    }

    public class DayMealPlan
    {
        public DateTime   Date  { get; set; }
        public List<Meal> Meals { get; set; } = new();

        public double TotalCalories { get { double t = 0; foreach (var m in Meals) t += m.TotalCalories; return t; } }
        public double TotalProtein  { get { double t = 0; foreach (var m in Meals) t += m.TotalProtein;  return t; } }
        public double TotalCarbs    { get { double t = 0; foreach (var m in Meals) t += m.TotalCarbs;    return t; } }
        public double TotalFat      { get { double t = 0; foreach (var m in Meals) t += m.TotalFat;      return t; } }
        public override string ToString() => Date.ToString("ddd, MMM d");
    }

    public class DietPlan
    {
        public string            PlanName         { get; set; } = "";
        public double            DailyCalorieGoal { get; set; }
        public double            ProteinGoalG     { get; set; }
        public double            CarbsGoalG       { get; set; }
        public double            FatGoalG         { get; set; }
        public List<DayMealPlan> Week             { get; set; } = new();
        public override string ToString() => PlanName;
    }

    public static class SeedData
    {
        /// <summary>
        /// Call this once from MainForm after creating both plans.
        /// Sets AppState references so all panels share the same objects.
        /// </summary>
        public static void InitAppState(WorkoutPlan workout, DietPlan diet)
        {
            AppState.CurrentWorkout = workout;
            AppState.CurrentDiet    = diet;
        }

        public static WorkoutPlan DefaultWorkout()
        {
            var plan = new WorkoutPlan { PlanName = "Indoor Strength — Beginner" };

            var d1 = new WorkoutDay { DayLabel = "Day 1 — Chest & Triceps", DayOfWeek = DayOfWeek.Monday, Difficulty = "Beginner" };
            d1.Exercises.Add(new Exercise { Name = "Bench Press",           Category = "Strength", Sets = 4, Reps = 10, WeightKg = 40 });
            d1.Exercises.Add(new Exercise { Name = "Incline Dumbbell Press",Category = "Strength", Sets = 3, Reps = 12, WeightKg = 14 });
            d1.Exercises.Add(new Exercise { Name = "Tricep Dips",           Category = "Strength", Sets = 3, Reps = 15 });
            d1.Exercises.Add(new Exercise { Name = "Cable Fly",             Category = "Strength", Sets = 3, Reps = 12, WeightKg = 10 });
            plan.Days.Add(d1);

            var d2 = new WorkoutDay { DayLabel = "Day 2 — Back & Biceps", DayOfWeek = DayOfWeek.Wednesday, Difficulty = "Beginner" };
            d2.Exercises.Add(new Exercise { Name = "Deadlift",   Category = "Strength", Sets = 4, Reps = 8,  WeightKg = 60 });
            d2.Exercises.Add(new Exercise { Name = "Pull-ups",   Category = "Strength", Sets = 3, Reps = 8 });
            d2.Exercises.Add(new Exercise { Name = "Bicep Curl", Category = "Strength", Sets = 3, Reps = 12, WeightKg = 12 });
            d2.Exercises.Add(new Exercise { Name = "Seated Row", Category = "Strength", Sets = 3, Reps = 12, WeightKg = 30 });
            plan.Days.Add(d2);

            var d3 = new WorkoutDay { DayLabel = "Day 3 — Legs & Cardio", DayOfWeek = DayOfWeek.Friday, Difficulty = "Beginner" };
            d3.Exercises.Add(new Exercise { Name = "Squat",    Category = "Strength", Sets = 5, Reps = 10, WeightKg = 50 });
            d3.Exercises.Add(new Exercise { Name = "Leg Press",Category = "Strength", Sets = 3, Reps = 12, WeightKg = 80 });
            d3.Exercises.Add(new Exercise { Name = "Plank",    Category = "Strength", Sets = 3, Reps = 1,  DurationMinutes = 1 });
            d3.Exercises.Add(new Exercise { Name = "Running",  Category = "Cardio",   DurationMinutes = 20 });
            plan.Days.Add(d3);

            return plan;
        }

        public static DietPlan DefaultDiet()
        {
            var plan = new DietPlan
            {
                PlanName         = "Balanced Muscle-Build Plan",
                DailyCalorieGoal = 2500,
                ProteinGoalG     = 160,
                CarbsGoalG       = 280,
                FatGoalG         = 75
            };

            int daysBack = ((int)DateTime.Today.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            DateTime monday = DateTime.Today.AddDays(-daysBack);

            for (int i = 0; i < 7; i++)
            {
                var day = new DayMealPlan { Date = monday.AddDays(i) };

                var b = new Meal { Type = MealType.Breakfast };
                b.Foods.Add(new FoodItem { Name = "Oats",      Calories = 300, ProteinG = 10, CarbsG = 54, FatG = 6,   ServingGrams = 80 });
                b.Foods.Add(new FoodItem { Name = "Banana",    Calories = 90,  ProteinG = 1,  CarbsG = 23, FatG = 0,   ServingGrams = 100 });
                b.Foods.Add(new FoodItem { Name = "Eggs (x2)", Calories = 140, ProteinG = 12, CarbsG = 1,  FatG = 10,  ServingGrams = 100 });
                day.Meals.Add(b);

                var l = new Meal { Type = MealType.Lunch };
                l.Foods.Add(new FoodItem { Name = "Grilled Chicken", Calories = 280, ProteinG = 50, CarbsG = 0,  FatG = 6,   ServingGrams = 150 });
                l.Foods.Add(new FoodItem { Name = "Brown Rice",      Calories = 220, ProteinG = 5,  CarbsG = 45, FatG = 2,   ServingGrams = 100 });
                l.Foods.Add(new FoodItem { Name = "Broccoli",        Calories = 55,  ProteinG = 4,  CarbsG = 10, FatG = 0.5, ServingGrams = 150 });
                day.Meals.Add(l);

                var d = new Meal { Type = MealType.Dinner };
                d.Foods.Add(new FoodItem { Name = "Salmon Fillet", Calories = 350, ProteinG = 40, CarbsG = 0,  FatG = 20, ServingGrams = 180 });
                d.Foods.Add(new FoodItem { Name = "Sweet Potato",  Calories = 180, ProteinG = 3,  CarbsG = 40, FatG = 0,  ServingGrams = 150 });
                d.Foods.Add(new FoodItem { Name = "Salad Mix",     Calories = 40,  ProteinG = 2,  CarbsG = 6,  FatG = 0,  ServingGrams = 100 });
                day.Meals.Add(d);

                var s = new Meal { Type = MealType.Snack };
                s.Foods.Add(new FoodItem { Name = "Protein Shake", Calories = 150, ProteinG = 25, CarbsG = 5, FatG = 3,  ServingGrams = 250 });
                s.Foods.Add(new FoodItem { Name = "Almonds",       Calories = 160, ProteinG = 6,  CarbsG = 6, FatG = 14, ServingGrams = 30 });
                day.Meals.Add(s);

                plan.Week.Add(day);
            }
            return plan;
        }
    }
}
