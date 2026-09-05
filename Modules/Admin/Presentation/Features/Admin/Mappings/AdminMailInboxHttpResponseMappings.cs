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
                model.ReceivedAtUtc,
                model.EnvelopeFromAddress,
                model.IsTrustedRelay);
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
                model.ContentPurgedAtUtc,
                model.DmarcReport?.ToHttpResponse(),
                model.EnvelopeFromAddress,
                model.IsTrustedRelay);
        }
    }

    extension(AdminMailInboxDmarcReportModel model) {
        private AdminMailInboxDmarcReportHttpResponse ToHttpResponse() =>
            new(
                model.OrganizationName,
                model.ReportId,
                model.Domain,
                model.DateRangeStartUtc,
                model.DateRangeEndUtc,
                model.Records.Select(static record => record.ToHttpResponse()).ToArray());
    }

    extension(AdminMailInboxDmarcRecordModel model) {
        private AdminMailInboxDmarcRecordHttpResponse ToHttpResponse() =>
            new(
                model.SourceIp,
                model.Count,
                model.Disposition,
                model.Dkim,
                model.Spf,
                model.HeaderFrom,
                model.EnvelopeFrom,
                model.DkimDomain,
                model.DkimResult,
                model.SpfDomain,
                model.SpfResult);
    }
}
