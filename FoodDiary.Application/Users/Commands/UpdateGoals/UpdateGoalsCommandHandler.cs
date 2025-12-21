using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Goals;

namespace FoodDiary.Application.Users.Commands.UpdateGoals;

public class UpdateGoalsCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateGoalsCommand, Result<GoalsResponse>>
{
    public async Task<Result<GoalsResponse>> Handle(UpdateGoalsCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId!.Value);
        if (user is null)
        {
            return Result.Failure<GoalsResponse>(User.NotFound(command.UserId.Value));
        }

        user.UpdateProfile(
            dailyCalorieTarget: command.DailyCalorieTarget,
            proteinTarget: command.ProteinTarget,
            fatTarget: command.FatTarget,
            carbTarget: command.CarbTarget,
            fiberTarget: command.FiberTarget,
            waterGoal: command.WaterGoal
        );

        if (command.DesiredWeight.HasValue)
        {
            user.UpdateDesiredWeight(command.DesiredWeight);
        }

        if (command.DesiredWaist.HasValue)
        {
            user.UpdateDesiredWaist(command.DesiredWaist);
        }

        await userRepository.UpdateAsync(user);

        return Result.Success(user.ToGoalsResponse());
    }
}
