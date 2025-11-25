using FoodDiary.Application.Users.Commands.ChangePassword;
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
            request.FirstName,
            request.LastName,
            request.BirthDate,
            request.Gender,
            request.Weight,
            request.Height,
            request.ActivityLevel,
            request.DailyCalorieTarget,
            request.ProteinTarget,
            request.FatTarget,
            request.CarbTarget,
            request.FiberTarget,
            request.StepGoal,
            request.WaterGoal,
            request.ProfileImage,
            request.IsActive
        );
    }

    public static ChangePasswordCommand ToCommand(this ChangePasswordRequest request, UserId? userId)
    {
        return new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );
    }
}
