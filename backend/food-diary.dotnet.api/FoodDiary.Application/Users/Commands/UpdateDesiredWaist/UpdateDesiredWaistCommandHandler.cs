using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Contracts.Users;

namespace FoodDiary.Application.Users.Commands.UpdateDesiredWaist;

public class UpdateDesiredWaistCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateDesiredWaistCommand, Result<UserDesiredWaistResponse>>
{
    public async Task<Result<UserDesiredWaistResponse>> Handle(
        UpdateDesiredWaistCommand command,
        CancellationToken cancellationToken)
    {
        if (command.UserId is null)
        {
            return Result.Failure<UserDesiredWaistResponse>(Errors.User.NotFound());
        }

        var user = await userRepository.GetByIdAsync(command.UserId.Value);
        if (user is null)
        {
            return Result.Failure<UserDesiredWaistResponse>(Errors.User.NotFound(command.UserId.Value));
        }

        user.UpdateDesiredWaist(command.DesiredWaist);
        await userRepository.UpdateAsync(user);

        return Result.Success(new UserDesiredWaistResponse(user.DesiredWaist));
    }
}
