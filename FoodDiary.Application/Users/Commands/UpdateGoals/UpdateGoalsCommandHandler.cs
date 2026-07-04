using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public sealed class UpdateGoalsCommandHandler(IUserContextService userContextService)
    : ICommandHandler<UpdateGoalsCommand, Result<GoalsModel>> {
    public async Task<Result<GoalsModel>> Handle(UpdateGoalsCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<GoalsModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
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
                DesiredWeight: command.DesiredWeight,
                DesiredWaist: command.DesiredWaist,
                CalorieCyclingEnabled: command.CalorieCyclingEnabled,
                MondayCalories: command.MondayCalories,
                TuesdayCalories: command.TuesdayCalories,
                WednesdayCalories: command.WednesdayCalories,
                ThursdayCalories: command.ThursdayCalories,
                FridayCalories: command.FridayCalories,
                SaturdayCalories: command.SaturdayCalories,
                SundayCalories: command.SundayCalories));
        } catch (ArgumentOutOfRangeException ex) {
            return Result.Failure<GoalsModel>(
                Errors.Validation.Invalid(ex.ParamName ?? nameof(UpdateGoalsCommand), ex.Message));
        }

        await userContextService.UpdateUserAsync(currentUser, cancellationToken).ConfigureAwait(false);

        return Result.Success(currentUser.ToGoalsModel());
    }
}
