using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminMailInboxHttpResponseMappings {
    extension(AdminMailInboxMessageSummaryModel model) {
        public AdminMailInboxMessageSummaryHttpResponse ToHttpResponse() {
            return new AdminMailInboxMessageSummaryHttpResponse(
                model.Id,
                model.FromAddress,
                model.ToRecipients,
                model.Subject,
                model.Category,
                model.Status,
                model.ReadAtUtc,
                model.ReceivedAtUtc);
        }
    }

    extension(AdminMailInboxMessageDetailsModel model) {
        public AdminMailInboxMessageDetailsHttpResponse ToHttpResponse() {
            return new AdminMailInboxMessageDetailsHttpResponse(
                model.Id,
                model.MessageId,
                model.FromAddress,
                model.ToRecipients,
                model.Subject,
                model.TextBody,
                model.HtmlBody,
                model.RawMime,
                model.Category,
                model.Status,
                model.ReadAtUtc,
                model.ReceivedAtUtc,
                model.ContentPurgedAtUtc);
        }
    }
}
