using FoodDiary.Modules.Users.Application.Commands.InitializePendingBodyGoals;
using FoodDiary.Modules.Users.Contracts.Commands.InitializePendingBodyGoals;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWaist;
using FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWeight;
using FoodDiary.Modules.Users.Application.Commands.UpdateGoals;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Entities.Tracking;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Enums;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

public partial class UsersFeatureTests {
    [Fact]
    public async Task UpdateGoals_WithoutCurrentMeasurements_ReplacesAnExistingMeasuredGoalUsingItsStoredBaseline() {
        var user = User.Create("retained-goal@example.test", "hash");
        user.StartWeightGoal(65, 72.8, DateTime.UtcNow.AddDays(-1));
        user.StartWaistGoal(75, 80.5, DateTime.UtcNow.AddDays(-1));
        ResultAssert.Success(await new UpdateGoalsCommandHandler(new SingleUserRepository(user)).Handle(
            new UpdateGoalsCommand(user.Id.Value, DailyCalorieTarget: null, ProteinTarget: null, FatTarget: null, CarbTarget: null, FiberTarget: null, WaterGoal: null, DesiredWeightKg: 64, DesiredWaistCm: 74), CancellationToken.None));
        WeightGoal weight = user.WeightGoals.Single(goal => goal.Status == WeightGoalStatus.Active);
        WaistGoal waist = user.WaistGoals.Single(goal => goal.Status == WaistGoalStatus.Active);
        Assert.Equal(64, weight.TargetWeightKg);
        Assert.Equal(72.8, weight.StartWeightKg);
        Assert.Equal(74, waist.TargetWaistCm);
        Assert.Equal(80.5, waist.StartWaistCm);
    }

    [Fact]
    public async Task DesiredGoals_BeforeFirstMeasurement_DoNotInventAStart_ThenStartOnceMeasured() {
        var user = User.Create("pending-goal@example.test", "hash");
        var users = new SingleUserRepository(user);
        Result<UserDesiredWeightModel> weight = await new UpdateDesiredWeightCommandHandler(users).Handle(new UpdateDesiredWeightCommand(user.Id.Value, 65), CancellationToken.None);
        Result<UserDesiredWaistModel> waist = await new UpdateDesiredWaistCommandHandler(users).Handle(new UpdateDesiredWaistCommand(user.Id.Value, 75), CancellationToken.None);
        Assert.Null(ResultAssert.Success(weight).StartWeightKg);
        Assert.Null(ResultAssert.Success(waist).StartWaistCm);
        Assert.Empty(user.WeightGoals);
        Assert.Empty(user.WaistGoals);
        var initialize = new InitializePendingBodyGoalsCommandHandler(users, TimeProvider.System);
        Assert.True(await initialize.Handle(new InitializePendingBodyGoalsCommand(user.Id, MeasuredWeightKg.Create(72.8), MeasuredWaistCm.Create(80.5)), CancellationToken.None));
        Assert.Equal(72.8, Assert.Single(user.WeightGoals).StartWeightKg);
        Assert.Equal(80.5, Assert.Single(user.WaistGoals).StartWaistCm);
        Assert.False(await initialize.Handle(new InitializePendingBodyGoalsCommand(user.Id, MeasuredWeightKg.Create(72), MeasuredWaistCm.Create(81.2)), CancellationToken.None));
        Assert.Equal(80.5, Assert.Single(user.WaistGoals).StartWaistCm);
    }

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
