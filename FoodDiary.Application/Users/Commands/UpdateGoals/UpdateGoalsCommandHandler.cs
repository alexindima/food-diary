using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public sealed class UpdateGoalsCommandHandler(
    IUserContextService userContextService,
    IUserCurrentWeightProvider currentWeightProvider,
    TimeProvider? timeProvider = null)
    : ICommandHandler<UpdateGoalsCommand, Result<GoalsModel>> {
    public UpdateGoalsCommandHandler(IUserContextService userContextService)
        : this(userContextService, NullCurrentWeightProvider.Instance) {
    }

    public async Task<Result<GoalsModel>> Handle(UpdateGoalsCommand command, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            userContextService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<GoalsModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (userResult.IsFailure) {
            return Result.Failure<GoalsModel>(userResult.Error);
        }

        User currentUser = userResult.Value;
        try {
            currentUser.UpdateGoals(new UserGoalUpdate(
                DailyCalorieTarget: command.DailyCalorieTarget,
                ProteinTarget: command.ProteinTarget,
                FatTarget: command.FatTarget,
                CarbTarget: command.CarbTarget,
                FiberTarget: command.FiberTarget,
                WaterGoal: command.WaterGoal,
                DesiredWeight: null,
                DesiredWaist: command.DesiredWaist,
                CalorieCyclingEnabled: command.CalorieCyclingEnabled,
                MondayCalories: command.MondayCalories,
                TuesdayCalories: command.TuesdayCalories,
                WednesdayCalories: command.WednesdayCalories,
                ThursdayCalories: command.ThursdayCalories,
                FridayCalories: command.FridayCalories,
                SaturdayCalories: command.SaturdayCalories,
                SundayCalories: command.SundayCalories));

            if (command.DesiredWeight.HasValue && command.DesiredWeight != currentUser.DesiredWeight) {
                double? trackedWeight = await currentWeightProvider
                    .GetCurrentWeightAsync(userId, cancellationToken)
                    .ConfigureAwait(false);
                double startWeight = trackedWeight ?? currentUser.Weight ?? command.DesiredWeight.Value;
                DateTime startedAtUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
                currentUser.StartWeightGoal(command.DesiredWeight.Value, startWeight, startedAtUtc);
            }
        } catch (ArgumentOutOfRangeException ex) {
            return Result.Failure<GoalsModel>(
                Errors.Validation.Invalid(ex.ParamName ?? nameof(UpdateGoalsCommand), ex.Message));
        }

        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(currentUser.ToGoalsModel());
    }

    private sealed class NullCurrentWeightProvider : IUserCurrentWeightProvider {
        public static readonly NullCurrentWeightProvider Instance = new();

        public Task<double?> GetCurrentWeightAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<double?>(null);
    }
}
