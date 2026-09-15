using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using FoodDiary.Modules.Users.Presentation.Contracts.Models;

namespace FoodDiary.Modules.Users.Presentation.Mappings.Mappings;

public static class UserSharedHttpResponseMappings {
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
                model.HasGoogleIdentity,
                model.SurfaceStyle,
                model.HasTelegramIdentity,
                model.TimeZoneId
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

    extension(DashboardLayoutModel model) {
        private DashboardLayoutHttpModel ToHttpModel()
                => new(model.Web, model.Mobile);
    }
}
