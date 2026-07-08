using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed class GetInboundMailMessageDetailsQueryHandler(IInboundMailStore store)
    : IRequestHandler<GetInboundMailMessageDetailsQuery, Result<InboundMailMessageDetails>> {
    public async Task<Result<InboundMailMessageDetails>> Handle(
        GetInboundMailMessageDetailsQuery request,
        CancellationToken cancellationToken) {
        InboundMailMessageDetails? message = await store.GetMessageDetailsAsync(request.Id, cancellationToken).ConfigureAwait(false);
        return message is null
            ? Result.Failure<InboundMailMessageDetails>(MailInboxErrors.MessageNotFound(request.Id))
            : Result.Success(message);
    }
}
