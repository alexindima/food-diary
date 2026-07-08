using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Commands;

public sealed class ReceiveInboundMailCommandHandler(IInboundMailStore store)
    : IRequestHandler<ReceiveInboundMailCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(ReceiveInboundMailCommand command, CancellationToken cancellationToken) {
        ReceiveInboundMailRequest request = command.Request;
        var message = InboundMailMessage.Receive(
            request.MessageId,
            request.FromAddress,
            request.ToRecipients,
            request.Subject,
            request.TextBody,
            request.HtmlBody,
            request.RawMime,
            request.ReceivedAtUtc);

        Guid id = await store.SaveAsync(message, cancellationToken).ConfigureAwait(false);
        return Result.Success(id);
    }
}
