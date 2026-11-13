using Microsoft.Extensions.Logging;

namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class MailRelayMessageProcessor(
    IMailRelayQueueStore queueStore,
    SmtpSubmissionService smtpSubmissionService,
    ILogger<MailRelayMessageProcessor> logger) {
    public async Task<MailRelayProcessResult> ProcessAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        if (!await queueStore.RenewClaimAsync(message.Id, message.AttemptCount, cancellationToken)) {
            return new MailRelayProcessResult.Recoverable();
        }
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        using var delivery = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, deadline.Token);
        using var renewalStop = new CancellationTokenSource();
        Task renewal = RenewClaimAsync(message, delivery, renewalStop.Token);
        try {
            return await ProcessClaimAsync(message, delivery.Token);
        } catch (MailRelayClaimLostException) {
            logger.LogInformation("Relay email {QueuedEmailId} is now owned by another delivery attempt.", message.Id);
            return new MailRelayProcessResult.Recoverable();
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            // Leave an expired/lost claim for recovery. No stale attempt may schedule a retry.
            return new MailRelayProcessResult.Recoverable();
        } finally {
            await renewalStop.CancelAsync();
            await renewal;
        }
    }

    private async Task RenewClaimAsync(QueuedEmailMessage message, CancellationTokenSource delivery, CancellationToken stoppingToken) {
        using var timer = new PeriodicTimer(queueStore.ClaimRenewalInterval);
        try {
            while (true) {
                await timer.WaitForNextTickAsync(stoppingToken);
                using var renewalTimeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                renewalTimeout.CancelAfter(queueStore.ClaimRenewalInterval);
                if (!await queueStore.RenewClaimAsync(message.Id, message.AttemptCount, renewalTimeout.Token)) {
                    await delivery.CancelAsync();
                    return;
                }
            }
        } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
        } catch (Exception exception) {
            logger.LogWarning("Relay email {QueuedEmailId} claim renewal failed ({ErrorType}).", message.Id, exception.GetType().Name);
            await delivery.CancelAsync();
        }
    }

    private async Task<MailRelayProcessResult> ProcessClaimAsync(QueuedEmailMessage message, CancellationToken cancellationToken) {
        var queuedEmail = QueuedEmail.FromPersistence(message);

        try {
            IReadOnlyList<string> suppressedRecipients = await queueStore.GetSuppressedRecipientsAsync(queuedEmail.To, cancellationToken);
            if (suppressedRecipients.Count > 0) {
                queuedEmail.MarkSuppressed();
                await queueStore.MarkSuppressedAsync(queuedEmail.Id, message.AttemptCount, suppressedRecipients, cancellationToken);
                logger.LogInformation(
                    "Relay email {QueuedEmailId} suppressed because {SuppressedRecipientCount} recipient(s) are on the suppression list.",
                    queuedEmail.Id,
                    suppressedRecipients.Count);
                MailRelayTelemetry.RecordDeliveryEvent("suppressed");
                return new MailRelayProcessResult.TerminalFailure();
            }

            await smtpSubmissionService.SendAsync(queuedEmail, cancellationToken);
            queuedEmail.MarkSent();
            // SMTP cannot atomically commit remote acceptance together with our
            // PostgreSQL state. Complete the local acknowledgement even when a
            // host shutdown is requested after SendAsync returns. A process
            // crash can still cause an at-least-once retry with the same stable
            // Message-Id, which is preferable to silently losing the email.
            using var acknowledgementTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await queueStore.MarkSentAsync(queuedEmail.Id, message.AttemptCount, acknowledgementTimeout.Token);
            logger.LogInformation(
                "Relay email {QueuedEmailId} sent successfully on attempt {AttemptCount}.",
                queuedEmail.Id,
                queuedEmail.AttemptCount);
            MailRelayTelemetry.RecordDeliveryEvent("success");
            return new MailRelayProcessResult.Sent();
        } catch (MailRelayClaimLostException) {
            throw;
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception ex) {
            string errorType = ex.GetType().Name;
            QueuedEmailFailureDecision failureDecision = queuedEmail.MarkFailedAttempt($"Delivery failed ({errorType}).");
            DateTimeOffset? retryAvailableAtUtc = await queueStore.MarkFailedAttemptAsync(failureDecision, cancellationToken);

            logger.LogWarning(
                "Relay email {QueuedEmailId} failed on attempt {AttemptCount}/{MaxAttempts}. ErrorType={ErrorType}",
                queuedEmail.Id,
                queuedEmail.AttemptCount,
                queuedEmail.MaxAttempts,
                errorType);
            MailRelayTelemetry.RecordDeliveryEvent("failure", errorType);
            return failureDecision.IsTerminalFailure
                ? new MailRelayProcessResult.TerminalFailure()
                : new MailRelayProcessResult.Retry(retryAvailableAtUtc);
        }
    }
}
