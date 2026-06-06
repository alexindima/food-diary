using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Presentation.Api.Features.Exercises.Mappings;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExerciseHttpMappingsTests {
    [Fact]
    public void CreateExerciseEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow;
        var request = new CreateExerciseEntryHttpRequest(date, "Running", 30, 250.5, "Morning jog", "Easy pace");

        CreateExerciseEntryCommand command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(date, command.Date);
        Assert.Equal("Running", command.ExerciseType);
        Assert.Equal(30, command.DurationMinutes);
        Assert.Equal(250.5, command.CaloriesBurned);
        Assert.Equal("Morning jog", command.Name);
        Assert.Equal("Easy pace", command.Notes);
    }

    [Fact]
    public void UpdateExerciseEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var request = new UpdateExerciseEntryHttpRequest("Swimming", 45, 300, Name: null, ClearName: true, "Pool", ClearNotes: false, Date: null);

        UpdateExerciseEntryCommand command = request.ToCommand(userId, entryId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.EntryId);
        Assert.Equal("Swimming", command.ExerciseType);
        Assert.Equal(45, command.DurationMinutes);
        Assert.True(command.ClearName);
        Assert.Equal("Pool", command.Notes);
    }

    [Fact]
    public void GetExerciseEntriesQuery_MapsDateRange() {
        var userId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-7);
        DateTime to = DateTime.UtcNow;

        GetExerciseEntriesQuery query = userId.ToQuery(from, to);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
    }
}
