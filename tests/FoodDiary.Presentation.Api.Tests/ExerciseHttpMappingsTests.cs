using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Presentation.Api.Features.Exercises.Mappings;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;
using FoodDiary.Presentation.Api.Features.Exercises.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExerciseHttpMappingsTests {
    [Fact]
    public void CreateExerciseEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow;
        var request = new CreateExerciseEntryHttpRequest(date, "Running", 30, 250.5, "Morning jog", "Easy pace");

        CreateExerciseEntryCommand command = request.ToCommand(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(date, command.Date),
            () => Assert.Equal("Running", command.ExerciseType),
            () => Assert.Equal(30, command.DurationMinutes),
            () => Assert.Equal(250.5, command.CaloriesBurned),
            () => Assert.Equal("Morning jog", command.Name),
            () => Assert.Equal("Easy pace", command.Notes));
    }

    [Fact]
    public void UpdateExerciseEntryRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var request = new UpdateExerciseEntryHttpRequest("Swimming", 45, 300, Name: null, ClearName: true, "Pool", ClearNotes: false, Date: null);

        UpdateExerciseEntryCommand command = request.ToCommand(userId, entryId);

        Assert.Multiple(
            () => Assert.Equal(userId, command.UserId),
            () => Assert.Equal(entryId, command.EntryId),
            () => Assert.Equal("Swimming", command.ExerciseType),
            () => Assert.Equal(45, command.DurationMinutes),
            () => Assert.True(command.ClearName),
            () => Assert.Equal("Pool", command.Notes));
    }

    [Fact]
    public void GetExerciseEntriesQuery_MapsDateRange() {
        var userId = Guid.NewGuid();
        DateTime from = DateTime.UtcNow.AddDays(-7);
        DateTime to = DateTime.UtcNow;

        GetExerciseEntriesQuery query = userId.ToQuery(from, to);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo));
    }

    [Fact]
    public void UserIdAndEntryId_ToDeleteCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        DeleteExerciseEntryCommand command = userId.ToDeleteCommand(entryId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.EntryId);
    }

    [Fact]
    public void ExerciseEntryModel_ToHttpResponse_MapsAllFields() {
        var entryId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow;
        var model = new ExerciseEntryModel(entryId, date, "Running", "Morning jog", 30, 250.5, "Easy pace");

        ExerciseEntryHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(entryId, response.Id),
            () => Assert.Equal(date, response.Date),
            () => Assert.Equal("Running", response.ExerciseType),
            () => Assert.Equal("Morning jog", response.Name),
            () => Assert.Equal(30, response.DurationMinutes),
            () => Assert.Equal(250.5, response.CaloriesBurned),
            () => Assert.Equal("Easy pace", response.Notes));
    }

    [Fact]
    public void ExerciseEntryModels_ToHttpResponse_MapsList() {
        IReadOnlyList<ExerciseEntryModel> models = [
            new ExerciseEntryModel(Guid.NewGuid(), DateTime.UtcNow, "Running", "Morning jog", 30, 250.5, "Easy pace"),
            new ExerciseEntryModel(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), "Yoga", Name: null, 45, 120, Notes: null),
        ];

        IReadOnlyList<ExerciseEntryHttpResponse> response = models.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(2, response.Count),
            () => Assert.Equal("Running", response[0].ExerciseType),
            () => Assert.Equal("Yoga", response[1].ExerciseType));
    }
}
