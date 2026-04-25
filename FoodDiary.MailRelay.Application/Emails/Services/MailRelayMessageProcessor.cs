using Microsoft.Extensions.Logging;

namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayMessageProcessor(
    IMailRelayQueueStore queueStore,
    SmtpSubmissionService smtpSubmissionService,
    ILogger<MailRelayMessageProcessor> logger) {
    public async Task<MailRelayProcessResult> ProcessAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        var queuedEmail = QueuedEmail.FromPersistence(message);

        try {
            var suppressedRecipients = await queueStore.GetSuppressedRecipientsAsync(queuedEmail.To, cancellationToken);
            if (suppressedRecipients.Count > 0) {
                queuedEmail.MarkSuppressed();
                await queueStore.MarkSuppressedAsync(queuedEmail.Id, suppressedRecipients, cancellationToken);
                logger.LogInformation(
                    "Relay email {QueuedEmailId} suppressed because recipient(s) are on suppression list: {Recipients}. CorrelationId={CorrelationId}",
                    queuedEmail.Id,
                    string.Join(", ", suppressedRecipients),
                    queuedEmail.CorrelationId);
                MailRelayTelemetry.RecordDeliveryEvent("suppressed");
                return new MailRelayProcessResult(false, true);
            }

            await smtpSubmissionService.SendAsync(queuedEmail, cancellationToken);
            queuedEmail.MarkSent();
            await queueStore.MarkSentAsync(queuedEmail.Id, cancellationToken);
            logger.LogInformation(
                "Relay email {QueuedEmailId} sent successfully on attempt {AttemptCount}. CorrelationId={CorrelationId}",
                queuedEmail.Id,
                queuedEmail.AttemptCount,
                queuedEmail.CorrelationId);
            MailRelayTelemetry.RecordDeliveryEvent("success");
            return new MailRelayProcessResult(true, false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            var failureDecision = queuedEmail.MarkFailedAttempt(ex.ToString());
            await queueStore.MarkFailedAttemptAsync(failureDecision, cancellationToken);

            logger.LogWarning(
                ex,
                "Relay email {QueuedEmailId} failed on attempt {AttemptCount}/{MaxAttempts}. CorrelationId={CorrelationId}",
                queuedEmail.Id,
                queuedEmail.AttemptCount,
                queuedEmail.MaxAttempts,
                queuedEmail.CorrelationId);
            MailRelayTelemetry.RecordDeliveryEvent("failure", ex.GetType().Name);
            return new MailRelayProcessResult(false, failureDecision.IsTerminalFailure);
        }
    }
}
