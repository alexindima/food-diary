using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpResponseMappings {
    public static AdminAuditEntryHttpResponse ToHttpResponse(this AdminAuditEntryModel model) =>
        new(
            model.Id,
            model.ActorUserId,
            model.SubjectClientUserId,
            model.Action,
            model.TargetType,
            model.TargetId,
            model.Metadata,
            model.CreatedAtUtc);

    public static AdminUserCreationHttpResponse ToHttpResponse(this AdminUserCreationModel model) =>
        new(model.User.ToHttpResponse(), model.TemporaryPassword, model.CredentialsEmailQueued);

    public static AdminUserHttpResponse ToHttpResponse(this AdminUserModel model) {
        return new AdminUserHttpResponse(
            model.Id,
            model.Email,
            model.HasPassword,
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
            model.CalorieCyclingEnabled,
            model.MondayCalories,
            model.TuesdayCalories,
            model.WednesdayCalories,
            model.ThursdayCalories,
            model.FridayCalories,
            model.SaturdayCalories,
            model.SundayCalories,
            model.ProfileImage,
            model.ProfileImageAssetId,
            model.DashboardLayoutJson,
            model.Language,
            model.Theme,
            model.UiStyle,
            model.PushNotificationsEnabled,
            model.FastingPushNotificationsEnabled,
            model.SocialPushNotificationsEnabled,
            model.FastingCheckInReminderHours,
            model.FastingCheckInFollowUpReminderHours,
            model.TelegramUserId,
            model.IsActive,
            model.IsEmailConfirmed,
            model.CreatedOnUtc,
            model.DeletedAt,
            model.LastLoginAtUtc,
            model.Roles,
            model.AiInputTokenLimit,
            model.AiOutputTokenLimit,
            model.AiConsentAcceptedAt,
            model.MustChangePassword
        );
    }

    public static AdminImpersonationStartHttpResponse ToHttpResponse(this AdminImpersonationStartModel model) {
        return new AdminImpersonationStartHttpResponse(
            model.AccessToken,
            model.TargetUserId,
            model.TargetEmail,
            model.ActorUserId,
            model.Reason);
    }

    public static AdminImpersonationSessionHttpResponse ToHttpResponse(this AdminImpersonationSessionReadModel model) {
        return new AdminImpersonationSessionHttpResponse(
            model.Id,
            model.ActorUserId,
            model.ActorEmail,
            model.TargetUserId,
            model.TargetEmail,
            model.Reason,
            model.ActorIpAddress,
            model.ActorUserAgent,
            model.StartedAtUtc);
    }

    public static AdminUserLoginEventHttpResponse ToHttpResponse(this AdminUserLoginEventModel model) {
        return new AdminUserLoginEventHttpResponse(
            model.Id,
            model.UserId,
            model.UserEmail,
            model.AuthProvider,
            model.MaskedIpAddress,
            model.UserAgent,
            model.BrowserName,
            model.BrowserVersion,
            model.OperatingSystem,
            model.DeviceType,
            model.LoggedInAtUtc);
    }

    public static AdminUserLoginDeviceSummaryHttpResponse ToHttpResponse(this AdminUserLoginDeviceSummaryModel model) {
        return new AdminUserLoginDeviceSummaryHttpResponse(
            model.Key,
            model.Count,
            model.LastSeenAtUtc);
    }

    public static AdminUserRoleAuditEventHttpResponse ToHttpResponse(this AdminUserRoleAuditEventReadModel model) {
        return new AdminUserRoleAuditEventHttpResponse(
            model.Id,
            model.UserId,
            model.RoleName,
            model.Action,
            model.ActorUserId,
            model.ActorEmail,
            model.Source,
            model.OccurredAtUtc);
    }

    public static AdminDashboardSummaryHttpResponse ToHttpResponse(this AdminDashboardSummaryModel model) {
        return new AdminDashboardSummaryHttpResponse(
            model.TotalUsers,
            model.ActiveUsers,
            model.PremiumUsers,
            model.DeletedUsers,
            model.PendingReportsCount,
            model.RecentUsers.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static PagedHttpResponse<AdminUserHttpResponse> ToHttpResponse(this PagedResponse<AdminUserModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static PagedHttpResponse<AdminImpersonationSessionHttpResponse> ToImpersonationSessionsHttpResponse(
        this PagedResponse<AdminImpersonationSessionReadModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static PagedHttpResponse<AdminUserLoginEventHttpResponse> ToLoginEventsHttpResponse(
        this PagedResponse<AdminUserLoginEventModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }
}
