namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class SmtpSubmissionService(IRelayDeliveryTransport relayDeliveryTransport) {
    public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        return relayDeliveryTransport.SendAsync(request, cancellationToken);
    }

    public Task SendAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        return SendAsync(
            new RelayEmailMessageRequest(
                message.FromAddress,
                message.FromName,
                message.To,
                message.Subject,
                message.HtmlBody,
                message.TextBody,
                message.CorrelationId),
            cancellationToken);
    }
}
