using System.Text.Json;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpMappings {
    public static UpdateUserCommand ToCommand(this UpdateUserHttpRequest request, UserId? userId) {
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
            request.StepGoal,
            request.HydrationGoal,
            request.Language,
            request.ProfileImage,
            request.ProfileImageAssetId,
            dashboardLayoutJson,
            request.IsActive
        );
    }

    public static ChangePasswordCommand ToCommand(this ChangePasswordHttpRequest request, UserId? userId) {
        return new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );
    }
}
