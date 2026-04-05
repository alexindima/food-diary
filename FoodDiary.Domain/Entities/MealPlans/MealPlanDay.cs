using FoodDiary.Domain.Common;
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
        if (dayNumber is <= 0 or > MaxDayNumber) {
            throw new ArgumentOutOfRangeException(nameof(dayNumber), $"Day number must be between 1 and {MaxDayNumber}.");
        }

        var day = new MealPlanDay {
            Id = MealPlanDayId.New(),
            MealPlanId = planId,
            DayNumber = dayNumber
        };
        day.SetCreated();
        return day;
    }

    public MealPlanMeal AddMeal(MealType mealType, RecipeId recipeId, int servings = 1) {
        var meal = MealPlanMeal.Create(Id, mealType, recipeId, servings);
        _meals.Add(meal);
        return meal;
    }
}
