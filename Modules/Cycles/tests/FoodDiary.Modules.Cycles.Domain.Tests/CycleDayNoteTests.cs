using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Cycles.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class CycleDayNoteTests {
    private static readonly DateOnly Date = new(2026, 10, 1);

    [Fact]
    public void NotesOnlyDay_CanBeCreatedAndUpdatedWithoutSymptoms() {
        var profile = CycleProfile.Create(UserId.New(), Date);
        profile.SetDayNotes(Date, "  First note  ");
        CycleDayNote original = Assert.Single(profile.DayNotes);

        profile.SetDayNotes(Date, "Changed note");

        CycleDayNote updated = Assert.Single(profile.DayNotes);
        Assert.Equal(original.Id, updated.Id);
        Assert.Equal("Changed note", updated.Notes);
        Assert.Empty(profile.BleedingEntries);
        Assert.Empty(profile.SymptomEntries);
        Assert.Empty(profile.FertilitySignals);
    }

    [Fact]
    public void OmittedNotes_PreserveTextAndExplicitClearRemovesIt() {
        var profile = CycleProfile.Create(UserId.New(), Date);
        profile.SetDayNotes(Date, "Keep");
        profile.SetDayNotes(Date, notes: null);
        Assert.Equal("Keep", Assert.Single(profile.DayNotes).Notes);

        profile.SetDayNotes(Date, notes: null, clearNotes: true);

        Assert.Empty(profile.DayNotes);
    }

    [Fact]
    public void ClearDay_RemovesNotesOnlyDayAndKeepsOtherDates() {
        var profile = CycleProfile.Create(UserId.New(), Date);
        profile.SetDayNotes(Date, "Today");
        profile.SetDayNotes(Date.AddDays(-1), "Yesterday");

        Assert.True(profile.ClearDay(Date));
        Assert.Equal("Yesterday", Assert.Single(profile.DayNotes).Notes);
        Assert.False(profile.ClearDay(Date));
    }

    [Fact]
    public void InvalidReplacement_DoesNotLoseExistingNote() {
        var profile = CycleProfile.Create(UserId.New(), Date);
        profile.SetDayNotes(Date, "Keep");

        Assert.Throws<ArgumentOutOfRangeException>(() => profile.SetDayNotes(Date, new string('a', CycleProfile.MaxNotesLength + 1)));
        Assert.Throws<ArgumentException>(() => profile.SetDayNotes(Date, "Conflict", clearNotes: true));
        Assert.Equal("Keep", Assert.Single(profile.DayNotes).Notes);
    }
}
