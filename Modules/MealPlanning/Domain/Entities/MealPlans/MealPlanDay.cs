using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.MealPlans;

public sealed class MealPlanDay : Entity<MealPlanDayId> {
    private const int MaxDayNumber = 31;

    public MealPlanId MealPlanId { get; private set; }
    public MealPlan MealPlan { get; private set; } = null!;
    public int DayNumber { get; private set; }

    private readonly List<MealPlanMeal> _meals = [];
    public IReadOnlyCollection<MealPlanMeal> Meals => _meals.AsReadOnly();

    private MealPlanDay() {
    }

    internal static MealPlanDay Create(MealPlanId planId, int dayNumber) {
        if (planId == MealPlanId.Empty) {
            throw new ArgumentException("Meal plan id is required.", nameof(planId));
        }

        if (dayNumber is <= 0 or > MaxDayNumber) {
            throw new ArgumentOutOfRangeException(nameof(dayNumber), $"Day number must be between 1 and {MaxDayNumber}.");
        }

        var day = new MealPlanDay {
            Id = MealPlanDayId.New(),
            MealPlanId = planId,
            DayNumber = dayNumber,
        };
        day.SetCreated();
        return day;
    }

    public MealPlanMeal AddMeal(MealType mealType, RecipeId recipeId, int servings = 1) =>
        AddMeal(mealType, recipeId, servings, markModified: true);

    internal MealPlanMeal AddMealOnCreation(MealType mealType, RecipeId recipeId, int servings) =>
        AddMeal(mealType, recipeId, servings, markModified: false);

    private MealPlanMeal AddMeal(MealType mealType, RecipeId recipeId, int servings, bool markModified) {
        var meal = MealPlanMeal.Create(Id, mealType, recipeId, servings);
        _meals.Add(meal);
        if (markModified) {
            SetModified();
        }

        return meal;
    }
}
