using FoodDiary.Mediator;

namespace FoodDiary.MailRelay.Application.Emails.Queries.GetOutgoingEmailJournal;

public sealed class GetOutgoingEmailJournalQueryHandler(IMailRelayJournalReader reader) : IRequestHandler<GetOutgoingEmailJournalQuery, Result<OutgoingEmailJournalPage>> {
    public async Task<Result<OutgoingEmailJournalPage>> Handle(GetOutgoingEmailJournalQuery request, CancellationToken cancellationToken) =>
        Result.Success(await reader.GetPageAsync(request.Page, request.Limit, request.Purpose, request.Status, request.Recipient, cancellationToken, request.FromUtc, request.ToUtc, request.Id, request.CorrelationId).ConfigureAwait(false));
}
