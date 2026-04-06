using FoodDiary.Presentation.Api.Features.MealPlans.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class MealPlanHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsUserIdAndDietType() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery("LowCarb");

        Assert.Equal(userId, query.UserId);
        Assert.Equal("LowCarb", query.DietType);
    }

    [Fact]
    public void ToGetByIdQuery_MapsIds() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var query = userId.ToGetByIdQuery(planId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(planId, query.PlanId);
    }

    [Fact]
    public void ToAdoptCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var command = userId.ToAdoptCommand(planId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(planId, command.PlanId);
    }

    [Fact]
    public void ToGenerateShoppingListCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        var command = userId.ToGenerateShoppingListCommand(planId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(planId, command.PlanId);
    }
}
