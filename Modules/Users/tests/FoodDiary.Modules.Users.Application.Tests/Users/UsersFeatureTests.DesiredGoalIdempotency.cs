using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Users;

public partial class UsersFeatureTests {
    [Fact]
    public async Task UpdateDesiredWeightHandler_WithUnchangedTarget_PreservesActiveGoal() {
        var user = User.Create("desired-weight-idempotent@example.com", "hash");
        WeightGoal goal = user.StartWeightGoal(72.5, 80, DateTime.UtcNow.AddDays(-1));
        var handler = new UpdateDesiredWeightCommandHandler(new SingleUserRepository(user));

        Result<UserDesiredWeightModel> result = await handler.Handle(
            new UpdateDesiredWeightCommand(user.Id.Value, 72.5),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Same(goal, Assert.Single(user.WeightGoals));
        Assert.Multiple(
            () => Assert.Equal(WeightGoalStatus.Active, goal.Status),
            () => Assert.Equal(goal.StartedAtUtc, result.Value.StartedAtUtc));
    }

    [Fact]
    public async Task UpdateDesiredWaistHandler_WithUnchangedTarget_PreservesActiveGoal() {
        var user = User.Create("desired-waist-idempotent@example.com", "hash");
        WaistGoal goal = user.StartWaistGoal(78.5, 85, DateTime.UtcNow.AddDays(-1));
        var handler = new UpdateDesiredWaistCommandHandler(new SingleUserRepository(user));

        Result<UserDesiredWaistModel> result = await handler.Handle(
            new UpdateDesiredWaistCommand(user.Id.Value, 78.5),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Same(goal, Assert.Single(user.WaistGoals));
        Assert.Multiple(
            () => Assert.Equal(WaistGoalStatus.Active, goal.Status),
            () => Assert.Equal(goal.StartedAtUtc, result.Value.StartedAtUtc));
    }
}
