using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public sealed class UpdateDesiredWeightCommandHandler(
    IUserContextService userContextService,
    IUserCurrentWeightProvider currentWeightProvider,
    TimeProvider? timeProvider = null)
    : ICommandHandler<UpdateDesiredWeightCommand, Result<UserDesiredWeightModel>> {
    public UpdateDesiredWeightCommandHandler(IUserContextService userContextService)
        : this(userContextService, NullCurrentWeightProvider.Instance, TimeProvider.System) {
    }

    public async Task<Result<UserDesiredWeightModel>> Handle(
        UpdateDesiredWeightCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserDesiredWeightModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserDesiredWeightModel>(userResult.Error);
        }

        User currentUser = userResult.Value;
        FoodDiary.Domain.Entities.Tracking.WeightGoal? activeGoal = currentUser.WeightGoals.SingleOrDefault(
            goal => goal.Status == FoodDiary.Domain.Enums.WeightGoalStatus.Active);
        if (command.DesiredWeightKg == currentUser.DesiredWeightKg) {
            return Result.Success(new UserDesiredWeightModel(currentUser.DesiredWeightKg, activeGoal?.StartWeightKg, activeGoal?.StartedAtUtc));
        }

        DateTime nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        double? trackedWeight = await currentWeightProvider
            .GetCurrentWeightAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        double? activeGoalStartWeight = activeGoal?.StartWeightKg;
        double currentWeight = trackedWeight ?? currentUser.WeightKg ?? activeGoalStartWeight ?? command.DesiredWeightKg ?? 1;
        if (command.DesiredWeightKg.HasValue) {
            currentUser.StartWeightGoal(command.DesiredWeightKg.Value, currentWeight, nowUtc);
        } else {
            currentUser.CancelWeightGoal(nowUtc, currentWeight);
        }
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        activeGoal = currentUser.WeightGoals.SingleOrDefault(
            goal => goal.Status == FoodDiary.Domain.Enums.WeightGoalStatus.Active);
        return Result.Success(new UserDesiredWeightModel(currentUser.DesiredWeightKg, activeGoal?.StartWeightKg, activeGoal?.StartedAtUtc));
    }

    private sealed class NullCurrentWeightProvider : IUserCurrentWeightProvider {
        public static readonly NullCurrentWeightProvider Instance = new();

        public Task<double?> GetCurrentWeightAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<double?>(null);
    }
}
