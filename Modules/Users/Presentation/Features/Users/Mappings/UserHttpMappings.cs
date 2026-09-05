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
using FoodDiary.Application.Users.Queries.GetWeightGoalHistory;
using FoodDiary.Application.Users.Queries.GetWaistGoalHistory;
using FoodDiary.Application.Users.Queries.GetUserById;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Requests;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

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
                IsActive: request.IsActive
            );
        }
    }

    extension(UpdateUserAppearanceHttpRequest request) {
        public UpdateUserAppearanceCommand ToCommand(Guid? userId) {
            return new UpdateUserAppearanceCommand(
                UserId: userId,
                Theme: request.Theme,
                UiStyle: request.UiStyle
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
