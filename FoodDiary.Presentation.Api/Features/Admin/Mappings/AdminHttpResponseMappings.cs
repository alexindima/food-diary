using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

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
            model.RecentUsers.Select(ToHttpResponse).ToList()
        );
    }

    public static AdminAiUsageSummaryHttpResponse ToHttpResponse(this AdminAiUsageSummaryModel model) {
        return new AdminAiUsageSummaryHttpResponse(
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens,
            model.ByDay.Select(ToHttpResponse).ToList(),
            model.ByOperation.Select(ToHttpResponse).ToList(),
            model.ByModel.Select(ToHttpResponse).ToList(),
            model.ByUser.Select(ToHttpResponse).ToList()
        );
    }

    public static PagedResponse<AdminUserHttpResponse> ToHttpResponse(this PagedResponse<AdminUserModel> response) {
        return new PagedResponse<AdminUserHttpResponse>(
            response.Data.Select(ToHttpResponse).ToList(),
            response.Page,
            response.Limit,
            response.TotalPages,
            response.TotalItems
        );
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
