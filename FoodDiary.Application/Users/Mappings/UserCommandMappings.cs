using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Contracts.Users;
using FoodDiary.Domain.ValueObjects;
using System.Text.Json;

namespace FoodDiary.Application.Users.Mappings;

public static class UserCommandMappings
{
    public static UpdateUserCommand ToCommand(this UpdateUserRequest request, UserId? userId)
    {
        var dashboardLayoutJson = request.DashboardLayout is null
            ? null
            : JsonSerializer.Serialize(request.DashboardLayout);

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
            request.HydrationGoal,
            request.Language,
            request.ProfileImage,
            request.ProfileImageAssetId,
            dashboardLayoutJson,
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
