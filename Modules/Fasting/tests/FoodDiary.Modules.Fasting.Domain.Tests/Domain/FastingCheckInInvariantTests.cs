using System.Globalization;
using FoodDiary.Domain.Entities.Tracking.Fasting;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public class FastingCheckInInvariantTests {
    [Fact]
    public void Create_WithValidValues_NormalizesSymptomsNotesAndTimestamp() {
        var occurrenceId = FastingOccurrenceId.New();
        var userId = UserId.New();
        var checkedInAtLocal = new DateTime(2026, 3, 27, 12, 30, 0, DateTimeKind.Local);

        var checkIn = FastingCheckIn.Create(
            occurrenceId,
            userId,
            hungerLevel: 2,
            energyLevel: 3,
            moodLevel: 4,
            symptoms: ["  headache  ", "HEADACHE", " tired ", " "],
            notes: "  Feeling okay  ",
            checkedInAtLocal);

        Assert.Multiple(
            () => Assert.NotEqual(FastingCheckInId.Empty, checkIn.Id),
            () => Assert.Equal(occurrenceId, checkIn.OccurrenceId),
            () => Assert.Equal(userId, checkIn.UserId),
            () => Assert.Equal(checkedInAtLocal.ToUniversalTime(), checkIn.CheckedInAtUtc),
            () => Assert.Equal(2, checkIn.HungerLevel),
            () => Assert.Equal(3, checkIn.EnergyLevel),
            () => Assert.Equal(4, checkIn.MoodLevel),
            () => Assert.Equal("headache,tired", checkIn.Symptoms),
            () => Assert.Equal("Feeling okay", checkIn.Notes),
            () => Assert.Equal(checkedInAtLocal.ToUniversalTime(), checkIn.CreatedOnUtc));
    }

    [Fact]
    public void Create_WithBlankSymptomsAndNotes_StoresNulls() {
        var checkIn = FastingCheckIn.Create(
            FastingOccurrenceId.New(),
            UserId.New(),
            hungerLevel: 1,
            energyLevel: 5,
            moodLevel: 3,
            symptoms: [" ", ""],
            notes: " ",
            DateTime.UtcNow);

        Assert.Null(checkIn.Symptoms);
        Assert.Null(checkIn.Notes);
    }

    [Fact]
    public void Create_WithNullSymptoms_StoresNull() {
        var checkIn = FastingCheckIn.Create(
            FastingOccurrenceId.New(),
            UserId.New(),
            hungerLevel: 1,
            energyLevel: 2,
            moodLevel: 3,
            symptoms: null,
            notes: null,
            DateTime.UtcNow);

        Assert.Null(checkIn.Symptoms);
    }

    [Fact]
    public void Create_WithEmptyIds_Throws() {
        Assert.Throws<ArgumentException>(() =>
            FastingCheckIn.Create(FastingOccurrenceId.Empty, UserId.New(), 1, 2, 3, symptoms: null, notes: null, DateTime.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            FastingCheckIn.Create(FastingOccurrenceId.New(), UserId.Empty, 1, 2, 3, symptoms: null, notes: null, DateTime.UtcNow));
    }

    [Theory]
    [InlineData(0, 2, 3)]
    [InlineData(6, 2, 3)]
    [InlineData(1, 0, 3)]
    [InlineData(1, 6, 3)]
    [InlineData(1, 2, 0)]
    [InlineData(1, 2, 6)]
    public void Create_WithInvalidScaleValues_Throws(int hungerLevel, int energyLevel, int moodLevel) {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingCheckIn.Create(
                FastingOccurrenceId.New(),
                UserId.New(),
                hungerLevel,
                energyLevel,
                moodLevel,
                symptoms: null,
                notes: null,
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithTooManySymptoms_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingCheckIn.Create(
                FastingOccurrenceId.New(),
                UserId.New(),
                hungerLevel: 1,
                energyLevel: 2,
                moodLevel: 3,
                symptoms: Enumerable.Range(1, 9).Select(index => string.Create(CultureInfo.InvariantCulture, $"symptom-{index}")),
                notes: null,
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithTooLongSymptoms_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingCheckIn.Create(
                FastingOccurrenceId.New(),
                UserId.New(),
                hungerLevel: 1,
                energyLevel: 2,
                moodLevel: 3,
                symptoms: [new string('s', 201)],
                notes: null,
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithTooLongNotes_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingCheckIn.Create(
                FastingOccurrenceId.New(),
                UserId.New(),
                hungerLevel: 1,
                energyLevel: 2,
                moodLevel: 3,
                symptoms: null,
                notes: new string('n', 501),
                DateTime.UtcNow));
    }

    [Fact]
    public void Create_WithUnspecifiedTimestamp_Throws() {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FastingCheckIn.Create(
                FastingOccurrenceId.New(),
                UserId.New(),
                hungerLevel: 1,
                energyLevel: 2,
                moodLevel: 3,
                symptoms: null,
                notes: null,
                new DateTime(2026, 3, 27)));
    }
}
