using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Users;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWeight;

public class UpdateDesiredWeightCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateDesiredWeightCommand, Result<UserDesiredWeightResponse>>
{
    public async Task<Result<UserDesiredWeightResponse>> Handle(
        UpdateDesiredWeightCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<UserDesiredWeightResponse>(Errors.User.NotFound());
        }

        var user = await userRepository.GetByIdAsync(command.UserId.Value);
        if (user is null)
        {
            return Result.Failure<UserDesiredWeightResponse>(Errors.User.NotFound(command.UserId.Value));
        }

        user.UpdateDesiredWeight(command.DesiredWeight);
        await userRepository.UpdateAsync(user);

        return Result.Success(new UserDesiredWeightResponse(user.DesiredWeight));
    }
}
