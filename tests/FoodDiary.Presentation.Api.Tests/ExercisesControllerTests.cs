using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Exercises.Commands.CreateExerciseEntry;
using FoodDiary.Application.Exercises.Commands.DeleteExerciseEntry;
using FoodDiary.Application.Exercises.Commands.UpdateExerciseEntry;
using FoodDiary.Application.Exercises.Models;
using FoodDiary.Application.Exercises.Queries.GetExerciseEntries;
using FoodDiary.Presentation.Api.Features.Exercises;
using FoodDiary.Presentation.Api.Features.Exercises.Requests;
using FoodDiary.Presentation.Api.Features.Exercises.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class ExercisesControllerTests {
    [Fact]
    public async Task GetAll_SendsQueryAndReturnsEntries() {
        ExerciseEntryModel model = CreateExercise();
        RecordingSender sender = new(Result.Success<IReadOnlyList<ExerciseEntryModel>>([model]));
        ExercisesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        DateTime dateFrom = DateTime.UtcNow.AddDays(-7);
        DateTime dateTo = DateTime.UtcNow;

        IActionResult result = await controller.GetAll(userId, dateFrom, dateTo);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        IReadOnlyList<ExerciseEntryHttpResponse> response = Assert.IsAssignableFrom<IReadOnlyList<ExerciseEntryHttpResponse>>(ok.Value);
        Assert.Single(response);
        GetExerciseEntriesQuery query = Assert.IsType<GetExerciseEntriesQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
        Assert.Equal(dateFrom, query.DateFrom);
        Assert.Equal(dateTo, query.DateTo);
    }

    [Fact]
    public async Task Create_SendsCommandAndReturnsCreatedResponse() {
        ExerciseEntryModel model = CreateExercise();
        RecordingSender sender = new(Result.Success(model));
        ExercisesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        DateTime date = DateTime.UtcNow;
        var request = new CreateExerciseEntryHttpRequest(date, "Running", 30, 250, "Jog", "Notes");

        IActionResult result = await controller.Create(userId, request);

        CreatedResult created = Assert.IsType<CreatedResult>(result);
        ExerciseEntryHttpResponse response = Assert.IsType<ExerciseEntryHttpResponse>(created.Value);
        Assert.Equal(model.Id, response.Id);
        CreateExerciseEntryCommand command = Assert.IsType<CreateExerciseEntryCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(date, command.Date);
    }

    [Fact]
    public async Task Update_SendsCommandAndReturnsResponse() {
        ExerciseEntryModel model = CreateExercise();
        RecordingSender sender = new(Result.Success(model));
        ExercisesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();
        var request = new UpdateExerciseEntryHttpRequest("Yoga", 45, 120, "Evening yoga", ClearName: false, "Stretch", ClearNotes: false, Date: null);

        IActionResult result = await controller.Update(userId, entryId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ExerciseEntryHttpResponse response = Assert.IsType<ExerciseEntryHttpResponse>(ok.Value);
        Assert.Equal(model.Id, response.Id);
        UpdateExerciseEntryCommand command = Assert.IsType<UpdateExerciseEntryCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.EntryId);
    }

    [Fact]
    public async Task Delete_SendsDeleteCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        ExercisesController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        IActionResult result = await controller.Delete(userId, entryId);

        Assert.IsType<NoContentResult>(result);
        DeleteExerciseEntryCommand command = Assert.IsType<DeleteExerciseEntryCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(entryId, command.EntryId);
    }

    private static ExercisesController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static ExerciseEntryModel CreateExercise() =>
        new(Guid.NewGuid(), DateTime.UtcNow, "Running", "Jog", 30, 250, "Notes");
}
