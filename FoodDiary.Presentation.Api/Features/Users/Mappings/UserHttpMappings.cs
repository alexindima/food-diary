using FoodDiary.Application.Users.Commands.AcceptAiConsent;
using FoodDiary.Application.Users.Commands.RevokeAiConsent;
using FoodDiary.Application.Users.Commands.UpdateUserAppearance;
using FoodDiary.Application.Users.Commands.ChangePassword;
using FoodDiary.Application.Users.Commands.SetPassword;
using FoodDiary.Application.Users.Commands.DeleteUser;
using FoodDiary.Application.Users.Commands.UpdateDesiredWaist;
using FoodDiary.Application.Users.Commands.UpdateDesiredWeight;
using FoodDiary.Application.Users.Commands.UpdateUser;
using FoodDiary.Application.Users.Queries.GetProfileOverview;
using FoodDiary.Application.Users.Queries.GetDesiredWaist;
using FoodDiary.Application.Users.Queries.GetDesiredWeight;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpMappings {
    extension(Guid userId) {
        public GetUserByIdQuery ToUserQuery() => new(userId);
        public GetProfileOverviewQuery ToProfileOverviewQuery() => new(userId);
        public GetDesiredWeightQuery ToDesiredWeightQuery() => new(userId);
        public GetDesiredWaistQuery ToDesiredWaistQuery() => new(userId);
    }

    public static UpdateDesiredWeightCommand ToDesiredWeightCommand(this UpdateDesiredWeightHttpRequest request, Guid userId) =>
        new(userId, request.DesiredWeight);

    public static UpdateDesiredWaistCommand ToDesiredWaistCommand(this UpdateDesiredWaistHttpRequest request, Guid userId) =>
        new(userId, request.DesiredWaist);

    extension(Guid userId) {
        public DeleteUserCommand ToDeleteCommand() => new(userId);
        public AcceptAiConsentCommand ToAcceptAiConsentCommand() => new(userId);
        public RevokeAiConsentCommand ToRevokeAiConsentCommand() => new(userId);
    }

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
            Theme: request.Theme,
            UiStyle: request.UiStyle,
            PushNotificationsEnabled: request.PushNotificationsEnabled,
            FastingPushNotificationsEnabled: request.FastingPushNotificationsEnabled,
            SocialPushNotificationsEnabled: request.SocialPushNotificationsEnabled,
            ProfileImage: request.ProfileImage,
            ProfileImageAssetId: request.ProfileImageAssetId,
            DashboardLayout: request.DashboardLayout?.ToModel(),
            IsActive: request.IsActive
        );
    }

    public static UpdateUserAppearanceCommand ToCommand(this UpdateUserAppearanceHttpRequest request, Guid? userId) {
        return new UpdateUserAppearanceCommand(
            UserId: userId,
            Theme: request.Theme,
            UiStyle: request.UiStyle
        );
    }

    public static ChangePasswordCommand ToCommand(this ChangePasswordHttpRequest request, Guid? userId) {
        return new ChangePasswordCommand(
            UserId: userId,
            CurrentPassword: request.CurrentPassword,
            NewPassword: request.NewPassword
        );
    }

    public static SetPasswordCommand ToCommand(this SetPasswordHttpRequest request, Guid? userId) {
        return new SetPasswordCommand(
            UserId: userId,
            NewPassword: request.NewPassword
        );
    }

    private static DashboardLayoutModel ToModel(this DashboardLayoutHttpModel model) =>
        new(model.Web, model.Mobile);
}
