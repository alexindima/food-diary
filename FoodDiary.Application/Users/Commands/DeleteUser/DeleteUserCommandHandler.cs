using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;

namespace FoodDiary.Application.Users.Commands.DeleteUser;

public class DeleteUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<DeleteUserCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(DeleteUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.UserId!.Value);
        if (user is null)
        {
            return Result.Failure<bool>(User.NotFound(command.UserId.Value));
        }

        user.MarkDeleted(DateTime.UtcNow);
        user.UpdateRefreshToken(null);
        await userRepository.UpdateAsync(user);

        return Result.Success(true);
    }
}
