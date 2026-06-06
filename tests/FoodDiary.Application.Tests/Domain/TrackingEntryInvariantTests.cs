using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class TrackingEntryInvariantTests {
    [Fact]
    public void HydrationEntry_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            HydrationEntry.Create(UserId.Empty, DateTime.UtcNow, 250));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void HydrationEntry_Create_WithInvalidAmount_Throws(int amountMl) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HydrationEntry.Create(UserId.New(), DateTime.UtcNow, amountMl));
    }

    [Fact]
    public void HydrationEntry_Create_WithLocalTimestamp_NormalizesToUtc() {
        var localTimestamp = new DateTime(2026, 3, 27, 14, 30, 0, DateTimeKind.Local);

        var entry = HydrationEntry.Create(UserId.New(), localTimestamp, 250);

        Assert.Equal(localTimestamp.ToUniversalTime(), entry.Timestamp);
        Assert.Equal(250, entry.AmountMl);
        Assert.NotEqual(HydrationEntryId.Empty, entry.Id);
    }

    [Fact]
    public void HydrationEntry_Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        DateTime timestamp = DateTime.UtcNow;
        var entry = HydrationEntry.Create(UserId.New(), timestamp, 250);

        entry.Update(amountMl: 250, timestampUtc: timestamp);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void HydrationEntry_Update_WithDifferentValues_SetsModifiedOnUtc() {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        DateTime newTimestamp = DateTime.UtcNow.AddMinutes(5);

        entry.Update(amountMl: 500, timestampUtc: newTimestamp);

        Assert.Equal(500, entry.AmountMl);
        Assert.Equal(newTimestamp, entry.Timestamp);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void HydrationEntry_Update_WithLocalTimestamp_NormalizesToUtc() {
        var entry = HydrationEntry.Create(UserId.New(), DateTime.UtcNow, 250);
        var localTimestamp = new DateTime(2026, 3, 27, 14, 30, 0, DateTimeKind.Local);

        entry.Update(timestampUtc: localTimestamp);

        Assert.Equal(localTimestamp.ToUniversalTime(), entry.Timestamp);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void ExerciseEntry_Create_NormalizesValues() {
        var localDate = new DateTime(2026, 3, 27, 14, 30, 0, DateTimeKind.Local);

        var entry = ExerciseEntry.Create(
            UserId.New(),
            localDate,
            ExerciseType.Cardio,
            durationMinutes: 45,
            caloriesBurned: 123.45,
            name: "  Run  ",
            notes: "  Easy pace  ");

        Assert.Equal(localDate.ToUniversalTime().Date, entry.Date);
        Assert.Equal(123.4, entry.CaloriesBurned);
        Assert.Equal("Run", entry.Name);
        Assert.Equal("Easy pace", entry.Notes);
    }

    [Fact]
    public void ExerciseEntry_Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ExerciseEntry.Create(UserId.Empty, DateTime.UtcNow, ExerciseType.Cardio, 30, 100));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void ExerciseEntry_Create_WithInvalidDuration_Throws(int durationMinutes) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Cardio, durationMinutes, 100));
    }

    [Theory]
    [InlineData(-1d)]
    [InlineData(10000.1d)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ExerciseEntry_Create_WithInvalidCalories_Throws(double caloriesBurned) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Cardio, 30, caloriesBurned));
    }

    [Fact]
    public void ExerciseEntry_Create_WithTooLongText_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Cardio, 30, 100, name: new string('n', 257)));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Cardio, 30, 100, notes: new string('n', 501)));
    }

    [Fact]
    public void ExerciseEntry_Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        DateTime date = DateTime.UtcNow.Date;
        var entry = ExerciseEntry.Create(UserId.New(), date, ExerciseType.Cardio, 30, 100, name: "Run", notes: "Easy");

        entry.Update(
            exerciseType: ExerciseType.Cardio,
            durationMinutes: 30,
            caloriesBurned: 100,
            name: "Run",
            notes: "Easy",
            date: date);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void ExerciseEntry_Update_WithDifferentValues_SetsModifiedOnUtc() {
        var entry = ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Cardio, 30, 100, name: "Run", notes: "Easy");
        DateTime newDate = DateTime.UtcNow.AddDays(-1);

        entry.Update(
            exerciseType: ExerciseType.Strength,
            durationMinutes: 45,
            caloriesBurned: 150.26,
            name: "  Lift  ",
            notes: "  Heavy  ",
            date: newDate);

        Assert.Equal(ExerciseType.Strength, entry.ExerciseType);
        Assert.Equal(45, entry.DurationMinutes);
        Assert.Equal(150.3, entry.CaloriesBurned);
        Assert.Equal("Lift", entry.Name);
        Assert.Equal("Heavy", entry.Notes);
        Assert.Equal(newDate.ToUniversalTime().Date, entry.Date);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void ExerciseEntry_Update_WithClearFlags_ClearsOptionalText() {
        var entry = ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Cardio, 30, 100, name: "Run", notes: "Easy");

        entry.Update(clearName: true, clearNotes: true);

        Assert.Null(entry.Name);
        Assert.Null(entry.Notes);
        Assert.NotNull(entry.ModifiedOnUtc);
    }
}
