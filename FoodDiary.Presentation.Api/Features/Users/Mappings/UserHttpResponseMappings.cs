using FoodDiary.Application.Users.Models;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Notifications.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Models;
using FoodDiary.Presentation.Api.Features.Users.Responses;

namespace FoodDiary.Presentation.Api.Features.Users.Mappings;

public static class UserHttpResponseMappings {
    public static UserHttpResponse ToHttpResponse(this UserModel model) {
        return new UserHttpResponse(
            model.Id,
            model.Email,
            model.Username,
            model.FirstName,
            model.LastName,
            model.BirthDate,
            model.Gender,
            model.Weight,
            model.DesiredWeight,
            model.DesiredWaist,
            model.Height,
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
            model.AiConsentAcceptedAt
        );
    }

    public static UserDesiredWeightHttpResponse ToHttpResponse(this UserDesiredWeightModel model)
        => new(model.DesiredWeight);

    public static UserDesiredWaistHttpResponse ToHttpResponse(this UserDesiredWaistModel model)
        => new(model.DesiredWaist);

    public static ProfileOverviewHttpResponse ToHttpResponse(this ProfileOverviewModel model) =>
        new(
            model.User.ToHttpResponse(),
            model.NotificationPreferences.ToHttpResponse(),
            model.WebPushSubscriptions.Select(static subscription => subscription.ToHttpResponse()).ToList(),
            model.DietologistRelationship?.ToHttpResponse());

    private static DashboardLayoutHttpModel ToHttpModel(this DashboardLayoutModel model)
        => new(model.Web, model.Mobile);
}
