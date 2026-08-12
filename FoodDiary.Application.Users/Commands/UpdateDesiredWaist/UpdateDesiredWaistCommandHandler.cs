using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

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
        DateTime nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        double? trackedWaist = await currentWaistProvider
            .GetCurrentWaistAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        double? activeGoalStartWaist = currentUser.WaistGoals.SingleOrDefault(
            goal => goal.Status == FoodDiary.Domain.Enums.WaistGoalStatus.Active)?.StartWaist;
        double currentWaist = trackedWaist ?? activeGoalStartWaist ?? command.DesiredWaist ?? 1;
        if (command.DesiredWaist.HasValue) {
            currentUser.StartWaistGoal(command.DesiredWaist.Value, currentWaist, nowUtc);
        } else {
            currentUser.CancelWaistGoal(nowUtc, currentWaist);
        }
        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        FoodDiary.Domain.Entities.Tracking.WaistGoal? activeGoal = currentUser.WaistGoals.SingleOrDefault(
            goal => goal.Status == FoodDiary.Domain.Enums.WaistGoalStatus.Active);
        return Result.Success(new UserDesiredWaistModel(currentUser.DesiredWaist, activeGoal?.StartWaist, activeGoal?.StartedAtUtc));
    }

    private sealed class NullCurrentWaistProvider : IUserCurrentWaistProvider {
        public static readonly NullCurrentWaistProvider Instance = new();

        public Task<double?> GetCurrentWaistAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<double?>(null);
    }
}
