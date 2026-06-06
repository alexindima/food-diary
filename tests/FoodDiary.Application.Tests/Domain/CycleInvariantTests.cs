using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
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
    public void UpdateLengths_WithClearNotes_ClearsNotes() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow, averageLength: 28, lutealLength: 14, notes: "notes");

        cycle.UpdateLengths(clearNotes: true);

        Assert.Null(cycle.Notes);
        Assert.NotNull(cycle.ModifiedOnUtc);
    }

    [Fact]
    public void UpdateLengths_WithChangedLengthsAndNotes_UpdatesState() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow, averageLength: 28, lutealLength: 14, notes: "notes");

        cycle.UpdateLengths(averageLength: 30, lutealLength: 15, notes: " updated ");

        Assert.Equal(30, cycle.AverageLength);
        Assert.Equal(15, cycle.LutealLength);
        Assert.Equal("updated", cycle.Notes);
        Assert.NotNull(cycle.ModifiedOnUtc);
    }

    [Fact]
    public void Create_WithWhitespaceNotes_NormalizesToNull() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow, notes: "   ");

        Assert.Null(cycle.Notes);
    }

    [Fact]
    public void UpdateLengths_WithClearNotesAndValue_Throws() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow, averageLength: 28, lutealLength: 14, notes: "notes");

        Assert.Throws<ArgumentException>(() => cycle.UpdateLengths(notes: "next", clearNotes: true));
    }

    [Theory]
    [InlineData(17, 14)]
    [InlineData(61, 14)]
    [InlineData(28, 7)]
    [InlineData(28, 19)]
    public void UpdateLengths_WithInvalidLengths_Throws(int averageLength, int lutealLength) {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            cycle.UpdateLengths(averageLength: averageLength, lutealLength: lutealLength));
    }

    [Fact]
    public void AddOrUpdateDay_WithSameValues_DoesNotRaiseDuplicateEvent() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        var symptoms = DailySymptoms.Create(1, 2, 3, 4, 5, 6, 7);
        DateTime date = DateTime.UtcNow.Date;
        cycle.AddOrUpdateDay(date, isPeriod: true, symptoms, notes: "  note  ");
        cycle.ClearDomainEvents();
        DateTime? modifiedOnUtc = cycle.ModifiedOnUtc;

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

    [Fact]
    public void AddOrUpdateDay_WithClearNotes_ClearsExistingNotes() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        var symptoms = DailySymptoms.Create(1, 2, 3, 4, 5, 6, 7);
        DateTime date = DateTime.UtcNow.Date;
        cycle.AddOrUpdateDay(date, isPeriod: true, symptoms, notes: "note");

        CycleDay day = cycle.AddOrUpdateDay(date, isPeriod: true, symptoms, clearNotes: true);

        Assert.Null(day.Notes);
    }

    [Fact]
    public void AddOrUpdateDay_WithChangedExistingDay_UpdatesDayAndCycle() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);
        var originalSymptoms = DailySymptoms.Create(1, 2, 3, 4, 5, 6, 7);
        var updatedSymptoms = DailySymptoms.Create(7, 6, 5, 4, 3, 2, 1);
        DateTime date = DateTime.UtcNow.Date;
        cycle.AddOrUpdateDay(date, isPeriod: true, originalSymptoms, notes: "note");
        cycle.ClearDomainEvents();

        CycleDay day = cycle.AddOrUpdateDay(date, isPeriod: false, updatedSymptoms, notes: " updated ");

        Assert.False(day.IsPeriod);
        Assert.Equal(updatedSymptoms, day.Symptoms);
        Assert.Equal("updated", day.Notes);
        Assert.NotNull(day.ModifiedOnUtc);
        Assert.NotEmpty(cycle.DomainEvents);
        Assert.NotNull(cycle.ModifiedOnUtc);
    }

    [Fact]
    public void CycleDay_Update_WithChangedValues_UpdatesState() {
        var symptoms = DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1);
        var updatedSymptoms = DailySymptoms.Create(2, 2, 2, 2, 2, 2, 2);
        var day = CycleDay.Create(CycleId.New(), DateTime.UtcNow, true, symptoms, "note");

        day.Update(isPeriod: false, symptoms: updatedSymptoms, notes: " updated ");

        Assert.False(day.IsPeriod);
        Assert.Equal(updatedSymptoms, day.Symptoms);
        Assert.Equal("updated", day.Notes);
        Assert.NotNull(day.ModifiedOnUtc);
    }

    [Fact]
    public void CycleDay_Update_WithClearNotesAndValue_Throws() {
        var symptoms = DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1);
        var day = CycleDay.Create(CycleId.New(), DateTime.UtcNow, true, symptoms, "note");

        Assert.Throws<ArgumentException>(() => day.Update(notes: "next", clearNotes: true));
    }

    [Fact]
    public void CycleDay_Create_WithNullSymptoms_Throws() {
        Assert.Throws<ArgumentNullException>(() =>
            CycleDay.Create(CycleId.New(), DateTime.UtcNow, true, null!, null));
    }

    [Fact]
    public void Create_WithLocalStartDate_NormalizesToUtcDate() {
        var localDate = new DateTime(2026, 3, 27, 23, 45, 0, DateTimeKind.Local);

        var cycle = Cycle.Create(UserId.New(), localDate);

        Assert.Equal(localDate.ToUniversalTime().Date, cycle.StartDate.Date);
        Assert.Equal(DateTimeKind.Utc, cycle.StartDate.Kind);
    }

    [Fact]
    public void Create_WithUnspecifiedStartDate_TreatsItAsUtcDateOnly() {
        var cycle = Cycle.Create(UserId.New(), new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified));

        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), cycle.StartDate);
    }

    [Fact]
    public void CycleDay_Create_WithUnspecifiedDate_TreatsItAsUtcDateOnly() {
        var symptoms = DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1);

        var day = CycleDay.Create(CycleId.New(), new DateTime(2026, 3, 27, 18, 45, 0, DateTimeKind.Unspecified), true, symptoms, null);

        Assert.Equal(new DateTime(2026, 3, 27, 0, 0, 0, DateTimeKind.Utc), day.Date);
    }

    [Fact]
    public void CycleDay_Create_WithWhitespaceNotes_NormalizesToNull() {
        var symptoms = DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1);

        var day = CycleDay.Create(CycleId.New(), DateTime.UtcNow, true, symptoms, "   ");

        Assert.Null(day.Notes);
    }

    [Fact]
    public void CycleDay_Update_WithClearNotesWhenAlreadyNull_DoesNotSetModifiedOnUtc() {
        var symptoms = DailySymptoms.Create(1, 1, 1, 1, 1, 1, 1);
        var day = CycleDay.Create(CycleId.New(), DateTime.UtcNow, true, symptoms, null);

        day.Update(clearNotes: true);

        Assert.Null(day.ModifiedOnUtc);
    }

    [Fact]
    public void RemoveDay_WhenDateDoesNotExist_ReturnsFalseAndDoesNotSetModifiedOnUtc() {
        var cycle = Cycle.Create(UserId.New(), DateTime.UtcNow);

        bool removed = cycle.RemoveDay(DateTime.UtcNow.AddDays(1));

        Assert.False(removed);
        Assert.Null(cycle.ModifiedOnUtc);
        Assert.Empty(cycle.DomainEvents);
    }
}
