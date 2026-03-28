using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
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
        if (user is null) {
            return Result.Failure<GoalsModel>(User.NotFound(userId));
        }

        user.UpdateGoals(new UserGoalUpdate(
            DailyCalorieTarget: command.DailyCalorieTarget,
            ProteinTarget: command.ProteinTarget,
            FatTarget: command.FatTarget,
            CarbTarget: command.CarbTarget,
            FiberTarget: command.FiberTarget,
            WaterGoal: command.WaterGoal,
            DesiredWeight: command.DesiredWeight,
            DesiredWaist: command.DesiredWaist));

        await userRepository.UpdateAsync(user, cancellationToken);

        return Result.Success(user.ToGoalsModel());
    }
}
