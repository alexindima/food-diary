using Microsoft.Extensions.Logging;

namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayMessageProcessor(
    IMailRelayQueueStore queueStore,
    SmtpSubmissionService smtpSubmissionService,
    ILogger<MailRelayMessageProcessor> logger) {
    public async Task<MailRelayProcessResult> ProcessAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        try {
            var suppressedRecipients = await queueStore.GetSuppressedRecipientsAsync(message.To, cancellationToken);
            if (suppressedRecipients.Count > 0) {
                await queueStore.MarkSuppressedAsync(message.Id, suppressedRecipients, cancellationToken);
                logger.LogInformation(
                    "Relay email {QueuedEmailId} suppressed because recipient(s) are on suppression list: {Recipients}. CorrelationId={CorrelationId}",
                    message.Id,
                    string.Join(", ", suppressedRecipients),
                    message.CorrelationId);
                MailRelayTelemetry.RecordDeliveryEvent("suppressed");
                return new MailRelayProcessResult(false, true);
            }

            await smtpSubmissionService.SendAsync(message, cancellationToken);
            await queueStore.MarkSentAsync(message.Id, cancellationToken);
            logger.LogInformation(
                "Relay email {QueuedEmailId} sent successfully on attempt {AttemptCount}. CorrelationId={CorrelationId}",
                message.Id,
                message.AttemptCount,
                message.CorrelationId);
            MailRelayTelemetry.RecordDeliveryEvent("success");
            return new MailRelayProcessResult(true, false);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            var failureResult = await queueStore.MarkFailedAttemptAsync(
                message.Id,
                message.AttemptCount,
                message.MaxAttempts,
                ex.ToString(),
                cancellationToken);

            logger.LogWarning(
                ex,
                "Relay email {QueuedEmailId} failed on attempt {AttemptCount}/{MaxAttempts}. CorrelationId={CorrelationId}",
                message.Id,
                message.AttemptCount,
                message.MaxAttempts,
                message.CorrelationId);
            MailRelayTelemetry.RecordDeliveryEvent("failure", ex.GetType().Name);
            return new MailRelayProcessResult(false, failureResult.IsTerminalFailure);
        }
    }
}
