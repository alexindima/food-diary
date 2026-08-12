using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpResponseMappings {
    extension(AdminAuditEntryModel model) {
        public AdminAuditEntryHttpResponse ToHttpResponse() =>
                new(
                    model.Id,
                    model.ActorUserId,
                    model.SubjectClientUserId,
                    model.Action,
                    model.TargetType,
                    model.TargetId,
                    model.Metadata,
                    model.CreatedAtUtc);
    }

    extension(AdminUserCreationModel model) {
        public AdminUserCreationHttpResponse ToHttpResponse() =>
                new(model.User.ToHttpResponse(), model.TemporaryPassword, model.CredentialsEmailQueued);
    }

    extension(AdminUserModel model) {
        public AdminUserHttpResponse ToHttpResponse() {
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
    }

    extension(AdminImpersonationStartModel model) {
        public AdminImpersonationStartHttpResponse ToHttpResponse() {
            return new AdminImpersonationStartHttpResponse(
                model.AccessToken,
                model.TargetUserId,
                model.TargetEmail,
                model.ActorUserId,
                model.Reason);
        }
    }

    extension(AdminImpersonationSessionReadModel model) {
        public AdminImpersonationSessionHttpResponse ToHttpResponse() {
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
    }

    extension(AdminUserLoginEventModel model) {
        public AdminUserLoginEventHttpResponse ToHttpResponse() {
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
    }

    extension(AdminUserLoginDeviceSummaryModel model) {
        public AdminUserLoginDeviceSummaryHttpResponse ToHttpResponse() {
            return new AdminUserLoginDeviceSummaryHttpResponse(
                model.Key,
                model.Count,
                model.LastSeenAtUtc);
        }
    }

    extension(AdminUserRoleAuditEventReadModel model) {
        public AdminUserRoleAuditEventHttpResponse ToHttpResponse() {
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
    }

    extension(AdminDashboardSummaryModel model) {
        public AdminDashboardSummaryHttpResponse ToHttpResponse() {
            return new AdminDashboardSummaryHttpResponse(
                model.TotalUsers,
                model.ActiveUsers,
                model.PremiumUsers,
                model.DeletedUsers,
                model.PendingReportsCount,
                model.RecentUsers.ToHttpResponseList(ToHttpResponse)
            );
        }
    }

    extension(PagedResponse<AdminUserModel> response) {
        public PagedHttpResponse<AdminUserHttpResponse> ToHttpResponse() {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }

    extension(PagedResponse<AdminImpersonationSessionReadModel> response) {
        public PagedHttpResponse<AdminImpersonationSessionHttpResponse> ToImpersonationSessionsHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }

    extension(PagedResponse<AdminUserLoginEventModel> response) {
        public PagedHttpResponse<AdminUserLoginEventHttpResponse> ToLoginEventsHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }
}
