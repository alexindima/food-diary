using FoodDiary.Modules.Meals.Domain.ValueObjects;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class DomainEventsTests {

    [Fact]
    public void AggregateRoot_ClearDomainEvents_EmptiesCollection() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(200, 10, 5, 20, 3, 0, IsAutoCalculated: true));
        Assert.NotEmpty(meal.DomainEvents);

        meal.ClearDomainEvents();

        Assert.Empty(meal.DomainEvents);
    }
}
