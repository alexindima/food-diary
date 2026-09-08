using FoodDiary.Application.Admin.Queries.GetAdminOutgoingEmails;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminOutgoingEmailHttpMappings {
    public static GetAdminOutgoingEmailsQuery ToQuery(this GetAdminOutgoingEmailsHttpQuery query) =>
        new(query.Page, query.Limit, query.Purpose, query.Status, query.Recipient, query.FromUtc, query.ToUtc, query.Id, query.CorrelationId);

    public static AdminOutgoingEmailPageHttpResponse ToHttpResponse(this OutgoingEmailJournalPage page) =>
        new(page.Items.Select(x => new AdminOutgoingEmailHttpResponse(x.Id, x.Status, x.Purpose, x.FromAddress, x.To,
            x.Subject, x.CreatedAtUtc, x.SentAtUtc, x.AttemptCount, x.MaxAttempts, x.CorrelationId, x.TextBody, x.ContentHidden, x.ReplyTo, x.InReplyTo)).ToList(), page.TotalItems, page.StatusCounts);
}
