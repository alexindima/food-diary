using FoodDiary.Modules.Users.Application.Commands.AcceptAiConsent;
using FoodDiary.Modules.Users.Application.Commands.RevokeAiConsent;
using FoodDiary.Modules.Users.Application.Commands.UpdateUserAppearance;
using FoodDiary.Modules.Users.Application.Commands.ChangePassword;
using FoodDiary.Modules.Users.Application.Commands.SetPassword;
using FoodDiary.Modules.Users.Application.Commands.DeleteUser;
using FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWaist;
using FoodDiary.Modules.Users.Application.Commands.UpdateDesiredWeight;
using FoodDiary.Modules.Users.Application.Commands.UpdateUser;
using FoodDiary.Modules.Users.Application.Queries.GetProfileOverview;
using FoodDiary.Modules.Users.Application.Queries.GetDesiredWaist;
using FoodDiary.Modules.Users.Application.Queries.GetDesiredWeight;
using FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistory;
using FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistory;
using FoodDiary.Modules.Users.Application.Queries.GetUserById;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Requests;

namespace FoodDiary.Modules.Users.Presentation.Mappings;

public static class UserHttpMappings {
    extension(Guid userId) {
        public GetUserByIdQuery ToUserQuery() => new(userId);
        public GetProfileOverviewQuery ToProfileOverviewQuery() => new(userId);
        public GetDesiredWeightQuery ToDesiredWeightQuery() => new(userId);
        public GetWeightGoalHistoryQuery ToWeightGoalHistoryQuery() => new(userId);
        public GetDesiredWaistQuery ToDesiredWaistQuery() => new(userId);
        public GetWaistGoalHistoryQuery ToWaistGoalHistoryQuery() => new(userId);
    }

    extension(UpdateDesiredWeightHttpRequest request) {
        public UpdateDesiredWeightCommand ToDesiredWeightCommand(Guid userId) =>
                new(userId, request.DesiredWeightKg);
    }

    extension(UpdateDesiredWaistHttpRequest request) {
        public UpdateDesiredWaistCommand ToDesiredWaistCommand(Guid userId) =>
                new(userId, request.DesiredWaistCm);
    }

    extension(Guid userId) {
        public DeleteUserCommand ToDeleteCommand() => new(userId);
        public AcceptAiConsentCommand ToAcceptAiConsentCommand() => new(userId);
        public RevokeAiConsentCommand ToRevokeAiConsentCommand() => new(userId);
    }

    extension(UpdateUserHttpRequest request) {
        public UpdateUserCommand ToCommand(Guid? userId) {
            return new UpdateUserCommand(
                UserId: userId,
                Username: request.Username,
                FirstName: request.FirstName,
                LastName: request.LastName,
                BirthDate: request.BirthDate,
                Gender: request.Gender,
                WeightKg: request.WeightKg,
                HeightCm: request.HeightCm,
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
                IsActive: request.IsActive,
                TimeZoneId: request.TimeZoneId
            );
        }
    }

    extension(UpdateUserAppearanceHttpRequest request) {
        public UpdateUserAppearanceCommand ToCommand(Guid? userId) {
            return new UpdateUserAppearanceCommand(
                UserId: userId,
                Theme: request.Theme,
                UiStyle: request.UiStyle,
                SurfaceStyle: request.SurfaceStyle
            );
        }
    }

    extension(ChangePasswordHttpRequest request) {
        public ChangePasswordCommand ToCommand(Guid? userId) {
            return new ChangePasswordCommand(
                UserId: userId,
                CurrentPassword: request.CurrentPassword,
                NewPassword: request.NewPassword
            );
        }
    }

    extension(SetPasswordHttpRequest request) {
        public SetPasswordCommand ToCommand(Guid? userId) {
            return new SetPasswordCommand(
                UserId: userId,
                NewPassword: request.NewPassword
            );
        }
    }

    extension(DashboardLayoutHttpModel model) {
        private DashboardLayoutModel ToModel() =>
                new(model.Web, model.Mobile);
    }
}
