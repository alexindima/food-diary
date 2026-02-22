using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class MealInvariantTests
{
    [Fact]
    public void ApplyNutrition_WithNegativeTotal_Throws()
    {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.BREAKFAST);

        Assert.Throws<ArgumentOutOfRangeException>(() => meal.ApplyNutrition(
            totalCalories: -1,
            totalProteins: 10,
            totalFats: 10,
            totalCarbs: 10,
            totalFiber: 1,
            totalAlcohol: 0,
            isAutoCalculated: true));
    }
}
