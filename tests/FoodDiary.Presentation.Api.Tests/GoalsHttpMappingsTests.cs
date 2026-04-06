using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Goals.Mappings;
using FoodDiary.Presentation.Api.Features.Goals.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class GoalsHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void UpdateGoalsRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var request = new UpdateGoalsHttpRequest(2000, 150, 70, 250, 30, 2500, 75, 80,
            true, 1800, 2000, 2200, 2000, 1800, 2200, 2000);

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(2000, command.DailyCalorieTarget);
        Assert.Equal(150, command.ProteinTarget);
        Assert.Equal(70, command.FatTarget);
        Assert.Equal(250, command.CarbTarget);
        Assert.Equal(30, command.FiberTarget);
        Assert.Equal(2500, command.WaterGoal);
        Assert.Equal(75, command.DesiredWeight);
        Assert.Equal(80, command.DesiredWaist);
        Assert.True(command.CalorieCyclingEnabled);
        Assert.Equal(1800, command.MondayCalories);
        Assert.Equal(2000, command.SundayCalories);
    }

    [Fact]
    public void GoalsModel_ToHttpResponse_MapsAllFields() {
        var model = new GoalsModel(2000, 150, 70, 250, 30, 2500, 75, 80,
            true, 1800, 2000, 2200, 2000, 1800, 2200, 2000);

        var response = model.ToHttpResponse();

        Assert.Equal(2000, response.DailyCalorieTarget);
        Assert.Equal(150, response.ProteinTarget);
        Assert.True(response.CalorieCyclingEnabled);
        Assert.Equal(1800, response.MondayCalories);
        Assert.Equal(2000, response.SundayCalories);
    }

    [Fact]
    public void GoalsModel_ToHttpResponse_WithNullValues() {
        var model = new GoalsModel(null, null, null, null, null, null, null, null,
            false, null, null, null, null, null, null, null);

        var response = model.ToHttpResponse();

        Assert.Null(response.DailyCalorieTarget);
        Assert.False(response.CalorieCyclingEnabled);
        Assert.Null(response.MondayCalories);
    }
}
