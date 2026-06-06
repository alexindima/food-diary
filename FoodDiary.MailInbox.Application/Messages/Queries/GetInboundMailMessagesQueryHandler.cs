using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed class GetInboundMailMessagesQueryHandler(IInboundMailStore store)
    : IRequestHandler<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>> {
    public async Task<Result<IReadOnlyList<InboundMailMessageSummary>>> Handle(
        GetInboundMailMessagesQuery query,
        CancellationToken cancellationToken) {
        IReadOnlyList<InboundMailMessageSummary> messages = await store.GetMessagesAsync(query.Limit, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<InboundMailMessageSummary>>.Success(messages);
    }
}
