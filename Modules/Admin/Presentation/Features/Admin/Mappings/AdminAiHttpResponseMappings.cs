using FoodDiary.Application.Admin.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminAiHttpResponseMappings {
    extension(AdminAiPromptModel model) {
        public AdminAiPromptHttpResponse ToAiPromptHttpResponse() {
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
    }

    extension(AdminAiUsageSummaryModel model) {
        public AdminAiUsageSummaryHttpResponse ToHttpResponse() {
            return new AdminAiUsageSummaryHttpResponse(
                model.TotalTokens,
                model.InputTokens,
                model.OutputTokens,
                model.ByDay.ToHttpResponseList(ToHttpResponse),
                model.ByOperation.ToHttpResponseList(ToHttpResponse),
                model.ByModel.ToHttpResponseList(ToHttpResponse),
                model.ByUser.ToHttpResponseList(ToHttpResponse));
        }
    }

    extension(AdminAiUsageDailyModel model) {
        private AdminAiUsageDailyHttpResponse ToHttpResponse() {
            return new AdminAiUsageDailyHttpResponse(
                model.Date,
                model.TotalTokens,
                model.InputTokens,
                model.OutputTokens);
        }
    }

    extension(AdminAiUsageBreakdownModel model) {
        private AdminAiUsageBreakdownHttpResponse ToHttpResponse() {
            return new AdminAiUsageBreakdownHttpResponse(
                model.Key,
                model.TotalTokens,
                model.InputTokens,
                model.OutputTokens);
        }
    }

    extension(AdminAiUsageUserModel model) {
        private AdminAiUsageUserHttpResponse ToHttpResponse() {
            return new AdminAiUsageUserHttpResponse(
                model.Id,
                model.Email,
                model.TotalTokens,
                model.InputTokens,
                model.OutputTokens);
        }
    }
}
