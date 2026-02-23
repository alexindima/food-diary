using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class CycleInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() => Cycle.Create(UserId.Empty, DateTime.UtcNow));
    }

    [Fact]
    public void UpdateLengths_WithNoEffectiveChanges_DoesNotSetModifiedOnUtc() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow, averageLength: 28, lutealLength: 14, notes: "notes");

        cycle.UpdateLengths(averageLength: 28, lutealLength: 14, notes: "  notes  ");

        Assert.Null(cycle.ModifiedOnUtc);
    }

    [Fact]
    public void AddOrUpdateDay_WithSameValues_DoesNotRaiseDuplicateEvent() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        var symptoms = DailySymptoms.Create(1, 2, 3, 4, 5, 6, 7);
        var date = DateTime.UtcNow.Date;
        cycle.AddOrUpdateDay(date, isPeriod: true, symptoms, notes: "  note  ");
        cycle.ClearDomainEvents();
        var modifiedOnUtc = cycle.ModifiedOnUtc;

        cycle.AddOrUpdateDay(date, isPeriod: true, symptoms, notes: "note");

        Assert.Empty(cycle.DomainEvents);
        Assert.Equal(modifiedOnUtc, cycle.ModifiedOnUtc);
    }

    [Fact]
    public void CycleDay_Create_WithEmptyCycleId_Throws() {
        var symptoms = DailySymptoms.Create(0, 0, 0, 0, 0, 0, 0);

        Assert.Throws<ArgumentException>(() => CycleDay.Create(CycleId.Empty, DateTime.UtcNow, true, symptoms, null));
    }

    [Fact]
    public void CycleDay_Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        var symptoms = DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1);
        var day = CycleDay.Create(CycleId.New(), DateTime.UtcNow, true, symptoms, "note");

        day.Update(isPeriod: true, symptoms: symptoms, notes: "  note  ");

        Assert.Null(day.ModifiedOnUtc);
    }
}
