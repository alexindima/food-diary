using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Messages.Queries;
using FoodDiary.MailInbox.Presentation.Features.Messages.Responses;

namespace FoodDiary.MailInbox.Presentation.Features.Messages.Mappings;

public static class InboundMailHttpMappings {
    public static GetInboundMailMessagesQuery ToQuery(this int? limit) => new(limit ?? 50);

    public static InboundMailMessageSummaryHttpResponse ToHttpResponse(this InboundMailMessageSummary message) =>
        new(
            message.Id,
            message.FromAddress,
            message.ToRecipients,
            message.Subject,
            message.Status,
            message.ReceivedAtUtc);

    public static InboundMailMessageDetailsHttpResponse ToHttpResponse(this InboundMailMessageDetails message) =>
        new(
            message.Id,
            message.MessageId,
            message.FromAddress,
            message.ToRecipients,
            message.Subject,
            message.TextBody,
            message.HtmlBody,
            message.RawMime,
            message.Status,
            message.ReceivedAtUtc);

    public static IReadOnlyList<InboundMailMessageSummaryHttpResponse> ToHttpResponse(
        this IReadOnlyList<InboundMailMessageSummary> messages) =>
        messages.Select(static message => message.ToHttpResponse()).ToArray();
}
