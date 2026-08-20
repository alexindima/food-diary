using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.MailInbox.Client;
using FoodDiary.MailInbox.Client.Models;

namespace FoodDiary.Integrations.Services.MailInbox;

internal sealed class MailInboxClientAdminMailInboxReader(IMailInboxClient mailInboxClient) : IAdminMailInboxReader {
    public async Task<IReadOnlyList<AdminMailInboxMessageSummaryModel>> GetMessagesAsync(
        int limit,
        CancellationToken cancellationToken) {
        IReadOnlyList<InboundMailMessageSummaryResponse> messages = await mailInboxClient.GetMessagesAsync(limit, cancellationToken).ConfigureAwait(false);
        return messages.Select(static message => message.ToModel()).ToList();
    }

    public async Task<AdminMailInboxMessageDetailsModel?> GetMessageAsync(
        Guid id,
        CancellationToken cancellationToken) {
        InboundMailMessageDetailsResponse? message = await mailInboxClient.GetMessageAsync(id, cancellationToken).ConfigureAwait(false);
        return message?.ToModel();
    }

    public Task<bool> MarkMessageReadAsync(
        Guid id,
        CancellationToken cancellationToken) {
        return mailInboxClient.MarkMessageReadAsync(id, cancellationToken);
    }
}

file static class MailInboxClientAdminMailInboxMappings {
    public static AdminMailInboxMessageSummaryModel ToModel(this InboundMailMessageSummaryResponse response) {
        return new AdminMailInboxMessageSummaryModel(
            response.Id,
            response.FromAddress,
            response.ToRecipients,
            response.Subject,
            response.Category,
            response.Status,
            response.ReadAtUtc,
            response.ReceivedAtUtc,
            response.EnvelopeFromAddress,
            response.IsTrustedRelay);
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
            response.Category,
            response.Status,
            response.ReadAtUtc,
            response.ReceivedAtUtc,
            response.ContentPurgedAtUtc,
            response.DmarcReport?.ToModel(),
            response.EnvelopeFromAddress,
            response.IsTrustedRelay);
    }

    private static AdminMailInboxDmarcReportModel ToModel(this DmarcReportResponse response) =>
        new(
            response.OrganizationName,
            response.ReportId,
            response.Domain,
            response.DateRangeStartUtc,
            response.DateRangeEndUtc,
            response.Records.Select(static record => record.ToModel()).ToArray());

    private static AdminMailInboxDmarcRecordModel ToModel(this DmarcReportRecordResponse response) =>
        new(
            response.SourceIp,
            response.Count,
            response.Disposition,
            response.Dkim,
            response.Spf,
            response.HeaderFrom,
            response.EnvelopeFrom,
            response.DkimDomain,
            response.DkimResult,
            response.SpfDomain,
            response.SpfResult);
}
