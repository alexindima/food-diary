using FoodDiary.Application.WeeklyGoals.Commands.UpsertWeeklyGoal;
using FoodDiary.Application.WeeklyGoals.Models;
using FoodDiary.Application.WeeklyGoals.Queries.GetWeeklyGoal;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.WeeklyGoals;
using FoodDiary.Presentation.Api.Features.WeeklyGoals.Mappings;
using FoodDiary.Presentation.Api.Features.WeeklyGoals.Requests;
using FoodDiary.Presentation.Api.Features.WeeklyGoals.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

#pragma warning disable MA0003

[ExcludeFromCodeCoverage]
public sealed class WeeklyGoalsControllerTests {
    private static readonly DateOnly WeekStart = new(2026, 8, 10);

    [Fact]
    public void Mappings_MapQueryCommandAndResponse() {
        var userId = Guid.NewGuid();
        var queryRequest = new GetWeeklyGoalHttpQuery(WeekStart);
        var upsertRequest = new UpsertWeeklyGoalHttpRequest(WeekStart, 5, true, new TimeOnly(9, 30), 240);
        WeeklyGoalModel model = CreateModel();

        GetWeeklyGoalQuery query = queryRequest.ToQuery(userId);
        UpsertWeeklyGoalCommand command = upsertRequest.ToCommand(userId);
        WeeklyGoalHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(WeekStart, query.WeekStart),
            () => Assert.Equal(5, command.TargetDays),
            () => Assert.Equal(new TimeOnly(9, 30), command.ReminderTime),
            () => Assert.Equal(model.Id, response.Id),
            () => Assert.Equal(model.ProgressDays, response.ProgressDays),
            () => Assert.Equal(model.TimeZoneOffsetMinutes, response.TimeZoneOffsetMinutes));
    }

    [Fact]
    public async Task Get_SendsQueryAndReturnsResponse() {
        IRequest<Result<WeeklyGoalModel?>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success<WeeklyGoalModel?>(CreateModel()), request => sentRequest = request);
        WeeklyGoalsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.Get(userId, new GetWeeklyGoalHttpQuery(WeekStart));

        WeeklyGoalHttpResponse response = Assert.IsType<WeeklyGoalHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(5, response.TargetDays);
        Assert.Equal(userId, Assert.IsType<GetWeeklyGoalQuery>(sentRequest).UserId);
    }

    [Fact]
    public async Task Get_WhenGoalDoesNotExist_ReturnsNoContent() {
        ISender sender = SubstituteSender.Create(Result.Success<WeeklyGoalModel?>(value: null));
        WeeklyGoalsController controller = CreateController(sender);

        IActionResult result = await controller.Get(Guid.NewGuid(), new GetWeeklyGoalHttpQuery(WeekStart));

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Upsert_SendsCommandAndReturnsResponse() {
        IRequest<Result<WeeklyGoalModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(CreateModel()), request => sentRequest = request);
        WeeklyGoalsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.Upsert(
            userId, new UpsertWeeklyGoalHttpRequest(WeekStart, 5, false, null, null));

        Assert.IsType<WeeklyGoalHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal(userId, Assert.IsType<UpsertWeeklyGoalCommand>(sentRequest).UserId);
    }

    private static WeeklyGoalsController CreateController(ISender sender) => new(sender) {
        ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
    };

    private static WeeklyGoalModel CreateModel() => new(
        Guid.NewGuid(), WeekStart, "DiaryLogging", 5, 3, false, true, new TimeOnly(9, 30), 240);
}
