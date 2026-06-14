using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Presentation.Api.Features.Goals;
using FoodDiary.Presentation.Api.Features.Goals.Requests;
using FoodDiary.Presentation.Api.Features.Goals.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class GoalsControllerTests {
    [Fact]
    public async Task GetGoals_SendsQueryAndReturnsGoals() {
        GoalsModel model = CreateGoals();
        RecordingSender sender = new(Result.Success(model));
        GoalsController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetGoals(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        GoalsHttpResponse response = Assert.IsType<GoalsHttpResponse>(ok.Value);
        Assert.Equal(2000, response.DailyCalorieTarget);
        GetUserGoalsQuery query = Assert.IsType<GetUserGoalsQuery>(sender.Request);
        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public async Task UpdateGoals_SendsCommandAndReturnsGoals() {
        GoalsModel model = CreateGoals();
        RecordingSender sender = new(Result.Success(model));
        GoalsController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var request = new UpdateGoalsHttpRequest(
            2000,
            150,
            70,
            250,
            30,
            2500,
            75,
            80,
            CalorieCyclingEnabled: true,
            MondayCalories: 1800,
            TuesdayCalories: 2000,
            WednesdayCalories: 2200,
            ThursdayCalories: 2000,
            FridayCalories: 1800,
            SaturdayCalories: 2200,
            SundayCalories: 2000);

        IActionResult result = await controller.UpdateGoals(userId, request);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        GoalsHttpResponse response = Assert.IsType<GoalsHttpResponse>(ok.Value);
        Assert.True(response.CalorieCyclingEnabled);
        UpdateGoalsCommand command = Assert.IsType<UpdateGoalsCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
        Assert.Equal(2000, command.DailyCalorieTarget);
        Assert.Equal(1800, command.MondayCalories);
    }

    private static GoalsController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static GoalsModel CreateGoals() =>
        new(
            2000,
            150,
            70,
            250,
            30,
            2500,
            75,
            80,
            CalorieCyclingEnabled: true,
            MondayCalories: 1800,
            TuesdayCalories: 2000,
            WednesdayCalories: 2200,
            ThursdayCalories: 2000,
            FridayCalories: 1800,
            SaturdayCalories: 2200,
            SundayCalories: 2000);
}
