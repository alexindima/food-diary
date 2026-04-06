using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Domain;

public class ExerciseEntryInvariantTests {
    [Fact]
    public void Create_WithEmptyUserId_Throws() {
        Assert.Throws<ArgumentException>(() =>
            ExerciseEntry.Create(UserId.Empty, DateTime.UtcNow, ExerciseType.Running, 30, 200));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void Create_WithInvalidDuration_Throws(int minutes) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Running, minutes, 200));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_WithInvalidCalories_Throws(double calories) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, calories));
    }

    [Fact]
    public void Create_WithZeroCalories_Succeeds() {
        var entry = ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 0);

        Assert.Equal(0, entry.CaloriesBurned);
    }

    [Fact]
    public void Create_RoundsCaloriesToOneDecimal() {
        var entry = ExerciseEntry.Create(UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 123.456);

        Assert.Equal(123.5, entry.CaloriesBurned);
    }

    [Fact]
    public void Create_NormalizesDateToUtcDate() {
        var localDate = new DateTime(2026, 3, 15, 14, 30, 0, DateTimeKind.Local);
        var entry = ExerciseEntry.Create(UserId.New(), localDate, ExerciseType.Running, 30, 100);

        Assert.Equal(DateTimeKind.Utc, entry.Date.Kind);
        Assert.Equal(TimeSpan.Zero, entry.Date.TimeOfDay);
    }

    [Fact]
    public void Create_TrimsOptionalName() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100, name: "  Morning Jog  ");

        Assert.Equal("Morning Jog", entry.Name);
    }

    [Fact]
    public void Create_WithTooLongName_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(
                UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100,
                name: new string('n', 257)));
    }

    [Fact]
    public void Create_WithTooLongNotes_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ExerciseEntry.Create(
                UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100,
                notes: new string('n', 501)));
    }

    [Fact]
    public void Create_WithWhitespaceName_SetsNull() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100, name: "   ");

        Assert.Null(entry.Name);
    }

    [Fact]
    public void Update_WithSameValues_DoesNotSetModifiedOnUtc() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100);

        entry.Update(exerciseType: ExerciseType.Running);

        Assert.Null(entry.ModifiedOnUtc);
    }

    [Fact]
    public void Update_WithNewDuration_SetsModifiedOnUtc() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100);

        entry.Update(durationMinutes: 45);

        Assert.NotNull(entry.ModifiedOnUtc);
        Assert.Equal(45, entry.DurationMinutes);
    }

    [Fact]
    public void Update_WithInvalidDuration_Throws() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100);

        Assert.Throws<ArgumentOutOfRangeException>(() => entry.Update(durationMinutes: 0));
    }

    [Fact]
    public void Update_ClearName_SetsNameToNull() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100, name: "Jog");

        entry.Update(clearName: true);

        Assert.Null(entry.Name);
        Assert.NotNull(entry.ModifiedOnUtc);
    }

    [Fact]
    public void Update_ClearNotes_SetsNotesToNull() {
        var entry = ExerciseEntry.Create(
            UserId.New(), DateTime.UtcNow, ExerciseType.Running, 30, 100, notes: "Easy run");

        entry.Update(clearNotes: true);

        Assert.Null(entry.Notes);
    }
}
