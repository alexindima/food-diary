using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public class UpdateGoalsCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateGoalsCommand, Result<GoalsModel>> {
    public async Task<Result<GoalsModel>> Handle(UpdateGoalsCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure<GoalsModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(command.UserId!.Value);
        var user = await userRepository.GetByIdAsync(userId, cancellationToken);
        var accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        if (accessError is not null) {
            return Result.Failure<GoalsModel>(accessError);
        }

        var currentUser = user!;
        currentUser.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: command.DailyCalorieTarget,
            ProteinTarget: command.ProteinTarget,
            FatTarget: command.FatTarget,
            CarbTarget: command.CarbTarget,
            FiberTarget: command.FiberTarget,
            WaterGoal: command.WaterGoal,
            DesiredWeight: command.DesiredWeight,
            DesiredWaist: command.DesiredWaist));

        await userRepository.UpdateAsync(currentUser, cancellationToken);

        return Result.Success(currentUser.ToGoalsModel());
    }
}
