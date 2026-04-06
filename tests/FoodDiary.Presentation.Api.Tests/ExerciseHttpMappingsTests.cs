using FoodDiary.Presentation.Api.Features.Exercises.Mappings;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ExerciseHttpMappingsTests {
    [Fact]
    public void CreateExerciseEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = DateTime.UtcNow;
        var request = new CreateExerciseEntryHttpRequest(date, "Running", 30, 250.5, "Morning jog", "Easy pace");

        var command = request.ToCommand(userId);

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
        var request = new UpdateExerciseEntryHttpRequest("Swimming", 45, 300, null, true, "Pool", false, null);

        var command = request.ToCommand(userId, entryId);

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
        var from = DateTime.UtcNow.AddDays(-7);
        var to = DateTime.UtcNow;

        var query = userId.ToQuery(from, to);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
    }
}
