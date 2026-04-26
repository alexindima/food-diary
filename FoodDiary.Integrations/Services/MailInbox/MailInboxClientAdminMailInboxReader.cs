using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.Integrations.Services.MailInbox;

internal sealed class MailInboxClientAdminMailInboxReader(IMailInboxClient mailInboxClient) : IAdminMailInboxReader {
    public async Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetMessagesAsync(
        int limit,
        CancellationToken cancellationToken) {
        var messages = await mailInboxClient.GetMessagesAsync(limit, cancellationToken);
        return messages.Select(static message => message.ToModel()).ToList();
    }

    public async Task<AdminMailInboxMessageDetailsModel?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken) {
        var message = await mailInboxClient.GetMessageAsync(id, cancellationToken);
        return message?.ToModel();
    }
}

file static class MailInboxClientAdminMailInboxMappings {
    public static AdminMailInboxMessageSummaryModel ToModel(this InboundMailMessageSummaryResponse response) {
        return new AdminMailInboxMessageSummaryModel(
            response.Id,
            response.FromAddress,
            response.ToRecipients,
            response.Subject,
            response.Status,
            response.ReceivedAtUtc);
    }

    public static AdminMailInboxMessageDetailsModel ToModel(this InboundMailMessageDetailsResponse response) {
        return new AdminMailInboxMessageDetailsModel(
            response.Id,
            response.MessageId,
            response.FromAddress,
            response.ToRecipients,
            response.Subject,
            response.TextBody,
            response.HtmlBody,
            response.RawMime,
            response.Status,
            response.ReceivedAtUtc);
    }
}
