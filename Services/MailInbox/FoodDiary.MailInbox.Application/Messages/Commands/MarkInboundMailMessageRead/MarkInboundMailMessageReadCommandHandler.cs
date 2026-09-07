using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Commands.MarkInboundMailMessageRead;

public sealed class MarkInboundMailMessageReadCommandHandler(
    IInboundMailStore store,
    TimeProvider timeProvider) : IRequestHandler<MarkInboundMailMessageReadCommand, Result> {
    public async Task<Result> Handle(MarkInboundMailMessageReadCommand request, CancellationToken cancellationToken) {
        bool updated = await store.MarkAsReadAsync(request.Id, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return updated
            ? Result.Success()
            : Result.Failure(MailInboxErrors.MessageNotFound(request.Id));
    }
}
