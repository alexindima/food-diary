using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class DomainEventsTests
{
    [Fact]
    public void User_MarkDeleted_AndRestore_RaisesEvents()
    {
        var user = User.Create("events@example.com", "hash");

        user.MarkDeleted(DateTime.UtcNow);
        user.Restore();

        Assert.Contains(user.DomainEvents, e => e is UserDeletedDomainEvent);
        Assert.Contains(user.DomainEvents, e => e is UserRestoredDomainEvent);
    }

    [Fact]
    public void Meal_ApplyNutrition_RaisesDomainEvent()
    {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.DINNER);

        meal.ApplyNutrition(
            totalCalories: 400,
            totalProteins: 20,
            totalFats: 10,
            totalCarbs: 40,
            totalFiber: 5,
            totalAlcohol: 0,
            isAutoCalculated: true);

        var evt = Assert.Single(meal.DomainEvents.OfType<MealNutritionAppliedDomainEvent>());
        Assert.Equal(meal.Id, evt.MealId);
        Assert.True(evt.IsAutoCalculated);
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_EmptiesCollection()
    {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.LUNCH);
        meal.ApplyNutrition(200, 10, 5, 20, 3, 0, true);
        Assert.NotEmpty(meal.DomainEvents);

        meal.ClearDomainEvents();

        Assert.Empty(meal.DomainEvents);
    }
}
