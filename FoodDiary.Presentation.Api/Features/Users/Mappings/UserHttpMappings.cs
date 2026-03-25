using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpMappings {
    public static GetUserByIdQuery ToUserQuery(this Guid userId) => new(userId);

    public static GetDesiredWeightQuery ToDesiredWeightQuery(this Guid userId) => new(userId);

    public static GetDesiredWaistQuery ToDesiredWaistQuery(this Guid userId) => new(userId);

    public static UpdateDesiredWeightCommand ToDesiredWeightCommand(this UpdateDesiredWeightHttpRequest request, Guid userId) =>
        new(userId, request.DesiredWeight);

    public static UpdateDesiredWaistCommand ToDesiredWaistCommand(this UpdateDesiredWaistHttpRequest request, Guid userId) =>
        new(userId, request.DesiredWaist);

    public static DeleteUserCommand ToDeleteCommand(this Guid userId) => new(userId);

    public static UpdateUserCommand ToCommand(this UpdateUserHttpRequest request, Guid? userId) {
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
            request.DashboardLayout?.ToModel(),
            request.IsActive
        );
    }

    public static ChangePasswordCommand ToCommand(this ChangePasswordHttpRequest request, Guid? userId) {
        return new ChangePasswordCommand(
            userId,
            request.CurrentPassword,
            request.NewPassword
        );
    }

    private static DashboardLayoutModel ToModel(this DashboardLayoutHttpModel model) =>
        new(model.Web, model.Mobile);
}
