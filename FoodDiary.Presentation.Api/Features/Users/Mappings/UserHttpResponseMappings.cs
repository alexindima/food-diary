using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpResponseMappings {
    extension(UserModel model) {
        public UserHttpResponse ToHttpResponse() {
            return new UserHttpResponse(
                model.Id,
                model.Email,
                model.HasPassword,
                model.Username,
                model.FirstName,
                model.LastName,
                model.BirthDate,
                model.Gender,
                model.WeightKg,
                model.DesiredWeightKg,
                model.DesiredWaistCm,
                model.HeightCm,
                model.ActivityLevel,
                model.DailyCalorieTarget,
                model.ProteinTarget,
                model.FatTarget,
                model.CarbTarget,
                model.FiberTarget,
                model.StepGoal,
                model.WaterGoal,
                model.HydrationGoal,
                model.Language,
                model.Theme,
                model.UiStyle,
                model.PushNotificationsEnabled,
                model.FastingPushNotificationsEnabled,
                model.SocialPushNotificationsEnabled,
                model.FastingCheckInReminderHours,
                model.FastingCheckInFollowUpReminderHours,
                model.ProfileImage,
                model.ProfileImageAssetId,
                model.DashboardLayout?.ToHttpModel(),
                model.IsActive,
                model.IsEmailConfirmed,
                model.LastLoginAtUtc,
                model.AiConsentAcceptedAt,
                model.MustChangePassword,
                model.HasGoogleIdentity
            );
        }
    }

    extension(UserDesiredWeightModel model) {
        public UserDesiredWeightHttpResponse ToHttpResponse()
                => new(model.DesiredWeightKg, model.StartWeightKg, model.StartedAtUtc);
    }

    extension(WeightGoalHistoryModel model) {
        public WeightGoalHistoryHttpResponse ToHttpResponse() =>
                new(model.Id, model.TargetWeightKg, model.StartWeightKg, model.EndWeightKg, model.StartedAtUtc, model.EndedAtUtc, model.Status);
    }

    extension(UserDesiredWaistModel model) {
        public UserDesiredWaistHttpResponse ToHttpResponse()
                => new(model.DesiredWaistCm, model.StartWaistCm, model.StartedAtUtc);
    }

    extension(WaistGoalHistoryModel model) {
        public WaistGoalHistoryHttpResponse ToHttpResponse() =>
                new(model.Id, model.TargetWaistCm, model.StartWaistCm, model.EndWaistCm, model.StartedAtUtc, model.EndedAtUtc, model.Status);
    }

    extension(ProfileOverviewModel model) {
        public ProfileOverviewHttpResponse ToHttpResponse() =>
                new(
                    model.User.ToHttpResponse(),
                    model.NotificationPreferences.ToHttpResponse(),
                    model.WebPushSubscriptions.Select(static subscription => subscription.ToHttpResponse()).ToList(),
                    model.DietologistRelationship?.ToHttpResponse());
    }

    extension(DashboardLayoutModel model) {
        private DashboardLayoutHttpModel ToHttpModel()
                => new(model.Web, model.Mobile);
    }
}
