namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class SmtpSubmissionService(IRelayDeliveryTransport relayDeliveryTransport) {
    public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        return relayDeliveryTransport.SendAsync(request, cancellationToken);
    }

    public Task SendAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        return SendAsync(QueuedEmail.FromPersistence(message), cancellationToken);
    }

    public Task SendAsync(QueuedEmail email, CancellationToken cancellationToken) {
        return SendAsync(email.ToSubmissionRequest(), cancellationToken);
    }
}
