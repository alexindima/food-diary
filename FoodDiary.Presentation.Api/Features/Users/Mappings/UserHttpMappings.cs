using FoodDiary.Application.Users.Commands.AcceptAiConsent;
using FoodDiary.Application.Users.Commands.RevokeAiConsent;
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

    public static AcceptAiConsentCommand ToAcceptAiConsentCommand(this Guid userId) => new(userId);

    public static RevokeAiConsentCommand ToRevokeAiConsentCommand(this Guid userId) => new(userId);

    public static UpdateUserCommand ToCommand(this UpdateUserHttpRequest request, Guid? userId) {
        return new UpdateUserCommand(
            UserId: userId,
            Username: request.Username,
            FirstName: request.FirstName,
            LastName: request.LastName,
            BirthDate: request.BirthDate,
            Gender: request.Gender,
            Weight: request.Weight,
            Height: request.Height,
            ActivityLevel: request.ActivityLevel,
            StepGoal: request.StepGoal,
            HydrationGoal: request.HydrationGoal,
            Language: request.Language,
            PushNotificationsEnabled: request.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: request.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: request.SocialPushNotificationsEnabled,
            ProfileImage: request.ProfileImage,
            ProfileImageAssetId: request.ProfileImageAssetId,
            DashboardLayout: request.DashboardLayout?.ToModel(),
            IsActive: request.IsActive
        );
    }

    public static ChangePasswordCommand ToCommand(this ChangePasswordHttpRequest request, Guid? userId) {
        return new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: request.CurrentPassword,
            NewPassword: request.NewPassword
        );
    }

    private static DashboardLayoutModel ToModel(this DashboardLayoutHttpModel model) =>
        new(model.Web, model.Mobile);
}
