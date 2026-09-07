using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessages;

public sealed class GetInboundMailMessagesQueryHandler(IInboundMailStore store)
    : IRequestHandler<GetInboundMailMessagesQuery, Result<IReadOnlyList<InboundMailMessageSummary>>> {
    public async Task<Result<IReadOnlyList<InboundMailMessageSummary>>> Handle(
        GetInboundMailMessagesQuery request,
        CancellationToken cancellationToken) {
        IReadOnlyList<InboundMailMessageSummary> messages = await store.GetMessagesAsync(request.Limit, cancellationToken).ConfigureAwait(false);
        return Result<IReadOnlyList<InboundMailMessageSummary>>.Success(messages);
    }
}
