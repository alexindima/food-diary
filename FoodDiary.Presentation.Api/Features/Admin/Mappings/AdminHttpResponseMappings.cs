using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpResponseMappings {
    public static AdminUserHttpResponse ToHttpResponse(this AdminUserModel model) {
        return new AdminUserHttpResponse(
            model.Id,
            model.Email,
            model.Username,
            model.FirstName,
            model.LastName,
            model.Language,
            model.IsActive,
            model.IsEmailConfirmed,
            model.CreatedOnUtc,
            model.DeletedAt,
            model.LastLoginAtUtc,
            model.Roles,
            model.AiInputTokenLimit,
            model.AiOutputTokenLimit
        );
    }

    public static AdminEmailTemplateHttpResponse ToHttpResponse(this AdminEmailTemplateModel model) {
        return new AdminEmailTemplateHttpResponse(
            model.Id,
            model.Key,
            model.Locale,
            model.Subject,
            model.HtmlBody,
            model.TextBody,
            model.IsActive,
            model.CreatedOnUtc,
            model.UpdatedOnUtc
        );
    }

    public static AdminDashboardSummaryHttpResponse ToHttpResponse(this AdminDashboardSummaryModel model) {
        return new AdminDashboardSummaryHttpResponse(
            model.TotalUsers,
            model.ActiveUsers,
            model.PremiumUsers,
            model.DeletedUsers,
            model.RecentUsers.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static AdminAiUsageSummaryHttpResponse ToHttpResponse(this AdminAiUsageSummaryModel model) {
        return new AdminAiUsageSummaryHttpResponse(
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens,
            model.ByDay.ToHttpResponseList(ToHttpResponse),
            model.ByOperation.ToHttpResponseList(ToHttpResponse),
            model.ByModel.ToHttpResponseList(ToHttpResponse),
            model.ByUser.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static PagedHttpResponse<AdminUserHttpResponse> ToHttpResponse(this PagedResponse<AdminUserModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    private static AdminAiUsageDailyHttpResponse ToHttpResponse(this AdminAiUsageDailyModel model) {
        return new AdminAiUsageDailyHttpResponse(
            model.Date,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens
        );
    }

    private static AdminAiUsageBreakdownHttpResponse ToHttpResponse(this AdminAiUsageBreakdownModel model) {
        return new AdminAiUsageBreakdownHttpResponse(
            model.Key,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens
        );
    }

    private static AdminAiUsageUserHttpResponse ToHttpResponse(this AdminAiUsageUserModel model) {
        return new AdminAiUsageUserHttpResponse(
            model.Id,
            model.Email,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens
        );
    }
}
