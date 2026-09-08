using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.Mediator;
using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;

namespace FoodDiary.MailInbox.Application.Messages.Queries.GetInboundMailMessagePage;

public sealed class GetInboundMailMessagePageQueryHandler(IInboundMailStore reader)
    : IRequestHandler<GetInboundMailMessagePageQuery, Result<InboundMailMessagePage>> {
    public async Task<Result<InboundMailMessagePage>> Handle(GetInboundMailMessagePageQuery request, CancellationToken cancellationToken) {
        InboundMailMessagePage page = await reader.GetMessagePageAsync(request.Page, request.Limit, request.Recipient, request.Category, request.Unread, cancellationToken).ConfigureAwait(false);
        return Result.Success(page);
    }
}

