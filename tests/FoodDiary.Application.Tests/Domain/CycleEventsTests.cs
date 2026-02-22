using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Domain;

public class CycleEventsTests
{
    [Fact]
    public void AddOrUpdateDay_RaisesUpsertEvent()
    {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        var symptoms = DailySymptoms.Create(0, 0, 0, 0, 0, 0, 0);

        cycle.AddOrUpdateDay(DateTime.UtcNow, isPeriod: true, symptoms);

        var evt = Assert.Single(cycle.DomainEvents.OfType<CycleDayUpsertedDomainEvent>());
        Assert.True(evt.IsCreated);
    }

    [Fact]
    public void RemoveDay_RaisesRemovedEvent()
    {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        var symptoms = DailySymptoms.Create(0, 0, 0, 0, 0, 0, 0);
        var date = DateTime.UtcNow.Date;
        cycle.AddOrUpdateDay(date, isPeriod: true, symptoms);
        cycle.ClearDomainEvents();

        var removed = cycle.RemoveDay(date);

        Assert.True(removed);
        Assert.Contains(cycle.DomainEvents, e => e is CycleDayRemovedDomainEvent);
    }
}

