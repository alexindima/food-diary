using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Users.Mappings;

public static class UserCommandMappings
{
    public static UpdateUserCommand ToCommand(this UpdateUserRequest request, UserId? userId)
    {
        return new UpdateUserCommand(
            userId,
            request.Username,
            request.Password,
            request.FirstName,
            request.LastName,
            request.BirthDate,
            request.Gender,
            request.Weight,
            request.Height,
            request.ProfileImage,
            request.IsActive
        );
    }
}
