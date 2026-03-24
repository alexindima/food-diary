using System.Text.Json;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpMappings {
    public static GetUserByIdQuery ToUserQuery(this UserId userId) => new(userId);

    public static GetDesiredWeightQuery ToDesiredWeightQuery(this UserId userId) => new(userId);

    public static GetDesiredWaistQuery ToDesiredWaistQuery(this UserId userId) => new(userId);

    public static UpdateDesiredWeightCommand ToDesiredWeightCommand(this UpdateDesiredWeightHttpRequest request, UserId userId) =>
        new(userId, request.DesiredWeight);

    public static UpdateDesiredWaistCommand ToDesiredWaistCommand(this UpdateDesiredWaistHttpRequest request, UserId userId) =>
        new(userId, request.DesiredWaist);

    public static DeleteUserCommand ToDeleteCommand(this UserId userId) => new(userId);

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
