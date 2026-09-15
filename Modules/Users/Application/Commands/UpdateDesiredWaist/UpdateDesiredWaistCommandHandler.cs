using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWaist;

public sealed class UpdateDesiredWaistCommandHandler(
    IUserContextService userContextService,
    IUserCurrentWaistProvider currentWaistProvider,
    TimeProvider? timeProvider = null)
    : ICommandHandler<UpdateDesiredWaistCommand, Result<UserDesiredWaistModel>> {
    public UpdateDesiredWaistCommandHandler(IUserContextService userContextService)
        : this(userContextService, NullCurrentWaistProvider.Instance, TimeProvider.System) {
    }

    public async Task<Result<UserDesiredWaistModel>> Handle(
        UpdateDesiredWaistCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<UserDesiredWaistModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<UserDesiredWaistModel>(userResult.Error);
        }

        User currentUser = userResult.Value;
        FoodDiary.Modules.Users.Domain.Entities.Tracking.WaistGoal? activeGoal = currentUser.WaistGoals.SingleOrDefault(
            goal => goal.Status == FoodDiary.Modules.Users.Domain.Enums.WaistGoalStatus.Active);
        if (command.DesiredWaistCm == currentUser.DesiredWaistCm) {
            return Result.Success(new UserDesiredWaistModel(currentUser.DesiredWaistCm, activeGoal?.StartWaistCm, activeGoal?.StartedAtUtc));
        }

        DateTime nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        double? trackedWaist = await currentWaistProvider
            .GetCurrentWaistAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        double? activeGoalStartWaist = activeGoal?.StartWaistCm;
        double currentWaist = trackedWaist ?? activeGoalStartWaist ?? command.DesiredWaistCm ?? 1;
        if (command.DesiredWaistCm.HasValue) {
            currentUser.StartWaistGoal(command.DesiredWaistCm.Value, currentWaist, nowUtc);
        } else {
            currentUser.CancelWaistGoal(nowUtc, currentWaist);
        }
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        activeGoal = currentUser.WaistGoals.SingleOrDefault(
            goal => goal.Status == FoodDiary.Modules.Users.Domain.Enums.WaistGoalStatus.Active);
        return Result.Success(new UserDesiredWaistModel(currentUser.DesiredWaistCm, activeGoal?.StartWaistCm, activeGoal?.StartedAtUtc));
    }

    private sealed class NullCurrentWaistProvider : IUserCurrentWaistProvider {
        public static readonly NullCurrentWaistProvider Instance = new();

        public Task<double?> GetCurrentWaistAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<double?>(null);
    }
}
