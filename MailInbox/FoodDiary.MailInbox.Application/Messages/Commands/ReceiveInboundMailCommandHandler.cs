using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.Results;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Domain.Messages;
using FoodDiary.Mediator;

namespace FoodDiary.MailInbox.Application.Messages.Commands;

public sealed class ReceiveInboundMailCommandHandler(IInboundMailStore store)
    : IRequestHandler<ReceiveInboundMailCommand, Result<Guid>> {
    public async Task<Result<Guid>> Handle(ReceiveInboundMailCommand request, CancellationToken cancellationToken) {
        ReceiveInboundMailRequest mailRequest = request.Request;
        var message = InboundMailMessage.Receive(
            mailRequest.MessageId,
            mailRequest.FromAddress,
            mailRequest.ToRecipients,
            mailRequest.Subject,
            mailRequest.TextBody,
            mailRequest.HtmlBody,
            mailRequest.RawMime,
            mailRequest.ReceivedAtUtc);

        InboundMailSaveResult saveResult = await store.SaveAsync(message, cancellationToken).ConfigureAwait(false);
        return Result.Success(saveResult.Id);
    }
}
