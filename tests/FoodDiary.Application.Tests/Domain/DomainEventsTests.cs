using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class DomainEventsTests {
    [Fact]
    public void User_MarkDeleted_AndRestore_RaisesEvents() {
        var user = User.Create("events@example.com", "hash");

        user.MarkDeleted(DateTime.UtcNow);
        user.Restore();

        Assert.Contains(user.DomainEvents, e => e is UserDeletedDomainEvent);
        Assert.Contains(user.DomainEvents, e => e is UserRestoredDomainEvent);
    }

    [Fact]
    public void Meal_ApplyNutrition_RaisesDomainEvent() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.Dinner);

        meal.ApplyNutrition(new MealNutritionUpdate(
            TotalCalories: 400,
            TotalProteins: 20,
            TotalFats: 10,
            TotalCarbs: 40,
            TotalFiber: 5,
            TotalAlcohol: 0,
            IsAutoCalculated: true));

        var evt = Assert.Single(meal.DomainEvents.OfType<MealNutritionAppliedDomainEvent>());
        Assert.Equal(meal.Id, evt.MealId);
        Assert.True(evt.IsAutoCalculated);
    }

    [Fact]
    public void AggregateRoot_ClearDomainEvents_EmptiesCollection() {
        var meal = Meal.Create(UserId.New(), DateTime.UtcNow, MealType.Lunch);
        meal.ApplyNutrition(new MealNutritionUpdate(200, 10, 5, 20, 3, 0, true));
        Assert.NotEmpty(meal.DomainEvents);

        meal.ClearDomainEvents();

        Assert.Empty(meal.DomainEvents);
    }
}
