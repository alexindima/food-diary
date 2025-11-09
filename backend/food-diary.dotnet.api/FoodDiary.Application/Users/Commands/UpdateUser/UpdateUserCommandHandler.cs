using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using static FoodDiary.Application.Common.Abstractions.Result.Errors;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Contracts.Users;

namespace FoodDiary.Application.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<UpdateUserCommand, Result<UserResponse>> {
    public async Task<Result<UserResponse>> Handle(UpdateUserCommand command, CancellationToken cancellationToken) {
        var user = await userRepository.GetByIdAsync(command.UserId!.Value);
        if (user is null) {
            return Result.Failure<UserResponse>(User.NotFound(command.UserId.Value));
        }

        user.UpdateProfile(
            username: command.Username,
            firstName: command.FirstName,
            lastName: command.LastName,
            birthDate: command.BirthDate,
            gender: command.Gender,
            weight: command.Weight,
            height: command.Height,
            profileImage: command.ProfileImage
        );

        if (command.IsActive.HasValue) {
            if (command.IsActive.Value)
                user.Activate();
            else
                user.Deactivate();
        }

        // TODO: Добавить метод для изменения пароля в User entity
        // if (command.Password != null)
        // {
        //     user.UpdatePassword(_passwordHasher.Hash(command.Password));
        // }

        await userRepository.UpdateAsync(user);

        return Result.Success(user.ToResponse());
    }
}