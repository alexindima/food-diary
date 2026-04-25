using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Common.Result;
using FoodDiary.MailInbox.Application.Messages.Models;
using MediatR;

namespace FoodDiary.MailInbox.Application.Messages.Queries;

public sealed class GetInboundMailMessageDetailsQueryHandler(IInboundMailStore store)
    : IRequestHandler<GetInboundMailMessageDetailsQuery, Result<InboundMailMessageDetails>> {
    public async Task<Result<InboundMailMessageDetails>> Handle(
        GetInboundMailMessageDetailsQuery query,
        CancellationToken cancellationToken) {
        var message = await store.GetMessageDetailsAsync(query.Id, cancellationToken);
        return message is null
            ? Result<InboundMailMessageDetails>.Failure(MailInboxErrors.MessageNotFound(query.Id))
            : Result<InboundMailMessageDetails>.Success(message);
    }
}
