using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminOutgoingEmails;

public sealed class GetAdminOutgoingEmailsQueryHandler(IOutgoingEmailJournal journal) : IQueryHandler<GetAdminOutgoingEmailsQuery, Result<OutgoingEmailJournalPage>> {
    public async Task<Result<OutgoingEmailJournalPage>> Handle(GetAdminOutgoingEmailsQuery query, CancellationToken cancellationToken) =>
        Result.Success(await journal.GetPageAsync(query.Page, query.Limit, query.Purpose, query.Status, query.Recipient, cancellationToken, query.FromUtc, query.ToUtc, query.Id, query.CorrelationId).ConfigureAwait(false));
}
