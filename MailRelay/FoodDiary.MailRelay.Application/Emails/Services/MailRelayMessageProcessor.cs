using Microsoft.Extensions.Logging;

namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayMessageProcessor(
    IMailRelayQueueStore queueStore,
    SmtpSubmissionService smtpSubmissionService,
    ILogger<MailRelayMessageProcessor> logger) {
    public async Task<MailRelayProcessResult> ProcessAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        var queuedEmail = QueuedEmail.FromPersistence(message);

        try {
            IReadOnlyList<string> suppressedRecipients = await queueStore.GetSuppressedRecipientsAsync(queuedEmail.To, cancellationToken).ConfigureAwait(false);
            if (suppressedRecipients.Count > 0) {
                queuedEmail.MarkSuppressed();
                await queueStore.MarkSuppressedAsync(queuedEmail.Id, suppressedRecipients, cancellationToken).ConfigureAwait(false);
                logger.LogInformation(
                    "Relay email {QueuedEmailId} suppressed because {SuppressedRecipientCount} recipient(s) are on the suppression list.",
                    queuedEmail.Id,
                    suppressedRecipients.Count);
                MailRelayTelemetry.RecordDeliveryEvent("suppressed");
                return new MailRelayProcessResult(Succeeded: false, IsTerminalFailure: true);
            }

            await smtpSubmissionService.SendAsync(queuedEmail, cancellationToken).ConfigureAwait(false);
            queuedEmail.MarkSent();
            // SMTP cannot atomically commit remote acceptance together with our
            // PostgreSQL state. Complete the local acknowledgement even when a
            // host shutdown is requested after SendAsync returns. A process
            // crash can still cause an at-least-once retry with the same stable
            // Message-Id, which is preferable to silently losing the email.
            await queueStore.MarkSentAsync(queuedEmail.Id, CancellationToken.None).ConfigureAwait(false);
            logger.LogInformation(
                "Relay email {QueuedEmailId} sent successfully on attempt {AttemptCount}.",
                queuedEmail.Id,
                queuedEmail.AttemptCount);
            MailRelayTelemetry.RecordDeliveryEvent("success");
            return new MailRelayProcessResult(Succeeded: true, IsTerminalFailure: false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            string errorType = ex.GetType().Name;
            QueuedEmailFailureDecision failureDecision = queuedEmail.MarkFailedAttempt($"Delivery failed ({errorType}).");
            DateTimeOffset? retryAvailableAtUtc = await queueStore.MarkFailedAttemptAsync(failureDecision, cancellationToken).ConfigureAwait(false);

            logger.LogWarning(
                "Relay email {QueuedEmailId} failed on attempt {AttemptCount}/{MaxAttempts}. ErrorType={ErrorType}",
                queuedEmail.Id,
                queuedEmail.AttemptCount,
                queuedEmail.MaxAttempts,
                errorType);
            MailRelayTelemetry.RecordDeliveryEvent("failure", errorType);
            return new MailRelayProcessResult(Succeeded: false, failureDecision.IsTerminalFailure, retryAvailableAtUtc);
        }
    }
}
