using FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleDay;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Tests;

public partial class CyclesFeatureTests {
    [Fact]
    public async Task UpsertDay_NotesOnly_ReturnsSavedTextAndSupportsExplicitClear() {
        var user = User.Create("cycle-notes@example.com", "hash");
        DateOnly date = new(2026, 4, 2);
        var profile = CycleProfile.Create(user.Id, date);
        var handler = new UpsertCycleDayCommandHandler(new InMemoryCycleRepository(profile), CreateCurrentUserAccessService(user));
        var command = new UpsertCycleDayCommand(user.Id.Value, profile.Id.Value, date,
            Bleeding: null, Symptoms: [], FertilitySignal: null, Notes: "  Quiet day  ");

        Result<CycleLogDayModel> result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Quiet day", result.Value.Notes);
        Assert.Empty(result.Value.BleedingEntries);
        Assert.Empty(result.Value.Symptoms);
        Assert.Null(result.Value.FertilitySignal);

        result = await handler.Handle(command with { Notes = null, ClearNotes = true }, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Notes);
        Assert.Empty(profile.DayNotes);
    }

    [Fact]
    public async Task UpsertDayValidator_RejectsOversizedNoteAndConflictingClear() {
        var validator = new UpsertCycleDayCommandValidator();
        var command = new UpsertCycleDayCommand(Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 4, 2),
            Bleeding: null, Symptoms: [], FertilitySignal: null, Notes: new string('a', CycleProfile.MaxNotesLength + 1));

        Assert.False((await validator.ValidateAsync(command)).IsValid);
        Assert.False((await validator.ValidateAsync(command with { Notes = "Conflict", ClearNotes = true })).IsValid);
    }
}
