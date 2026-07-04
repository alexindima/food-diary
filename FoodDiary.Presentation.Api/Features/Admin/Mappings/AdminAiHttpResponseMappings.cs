using FoodDiary.Application.Admin.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminAiHttpResponseMappings {
    public static AdminAiPromptHttpResponse ToAiPromptHttpResponse(this AdminAiPromptModel model) {
        return new AdminAiPromptHttpResponse(
            model.Id,
            model.Key,
            model.Locale,
            model.PromptText,
            model.Version,
            model.IsActive,
            model.CreatedOnUtc,
            model.UpdatedOnUtc);
    }

    public static AdminAiUsageSummaryHttpResponse ToHttpResponse(this AdminAiUsageSummaryModel model) {
        return new AdminAiUsageSummaryHttpResponse(
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens,
            model.ByDay.ToHttpResponseList(ToHttpResponse),
            model.ByOperation.ToHttpResponseList(ToHttpResponse),
            model.ByModel.ToHttpResponseList(ToHttpResponse),
            model.ByUser.ToHttpResponseList(ToHttpResponse));
    }

    private static AdminAiUsageDailyHttpResponse ToHttpResponse(this AdminAiUsageDailyModel model) {
        return new AdminAiUsageDailyHttpResponse(
            model.Date,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens);
    }

    private static AdminAiUsageBreakdownHttpResponse ToHttpResponse(this AdminAiUsageBreakdownModel model) {
        return new AdminAiUsageBreakdownHttpResponse(
            model.Key,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens);
    }

    private static AdminAiUsageUserHttpResponse ToHttpResponse(this AdminAiUsageUserModel model) {
        return new AdminAiUsageUserHttpResponse(
            model.Id,
            model.Email,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens);
    }
}
