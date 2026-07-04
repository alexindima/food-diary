using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminMailInboxHttpResponseMappings {
    public static AdminMailInboxMessageSummaryHttpResponse ToHttpResponse(this AdminMailInboxMessageSummaryModel model) {
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

    public static AdminMailInboxMessageDetailsHttpResponse ToHttpResponse(this AdminMailInboxMessageDetailsModel model) {
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
            model.ReceivedAtUtc);
    }
}
