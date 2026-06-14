using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Common.Results;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Commands;

public sealed class MarkInboundMailMessageReadCommandHandler(
    IInboundMailStore store,
    TimeProvider timeProvider) : IRequestHandler<MarkInboundMailMessageReadCommand, Result> {
    public async Task<Result> Handle(MarkInboundMailMessageReadCommand command, CancellationToken cancellationToken) {
        bool updated = await store.MarkAsReadAsync(command.Id, timeProvider.GetUtcNow(), cancellationToken).ConfigureAwait(false);
        return updated
            ? Result.Success()
            : Result.Failure(MailInboxErrors.MessageNotFound(command.Id));
    }
}
