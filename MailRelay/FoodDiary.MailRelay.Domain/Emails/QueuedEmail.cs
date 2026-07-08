using FoodDiary.Domain.Primitives;

namespace FoodDiary.MailRelay.Domain.Emails;

public sealed class QueuedEmail : AggregateRoot<QueuedEmailId> {
    private QueuedEmail(
        QueuedEmailId id,
        string fromAddress,
        string fromName,
        IReadOnlyList<string> to,
        string subject,
        string htmlBody,
        string? textBody,
        string? correlationId,
        int attemptCount,
        int maxAttempts,
        string status) : base(id) {
        FromAddress = fromAddress;
        FromName = fromName;
        To = to;
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        CorrelationId = correlationId;
        AttemptCount = attemptCount;
        MaxAttempts = maxAttempts;
        Status = status;
    }

    public string FromAddress { get; }
    public string FromName { get; }
    public IReadOnlyList<string> To { get; }
    public string Subject { get; }
    public string HtmlBody { get; }
    public string? TextBody { get; }
    public string? CorrelationId { get; }
    public int AttemptCount { get; }
    public int MaxAttempts { get; }
    public string Status { get; private set; }

    public static QueuedEmail FromPersistence(QueuedEmailMessage message) {
        var email = new QueuedEmail(
            (QueuedEmailId)message.Id,
            message.FromAddress,
            message.FromName,
            message.To,
            message.Subject,
            message.HtmlBody,
            message.TextBody,
            message.CorrelationId,
            message.AttemptCount,
            message.MaxAttempts,
            QueuedEmailStatus.Processing);
        email.SetCreated(message.CreatedAtUtc?.UtcDateTime ?? DomainTime.UtcNow);
        if (message.ModifiedAtUtc is { } modifiedAtUtc) {
            email.SetModified(modifiedAtUtc.UtcDateTime);
        }

        return email;
    }

    public RelayEmailMessageRequest ToSubmissionRequest() =>
        new(
            FromAddress,
            FromName,
            To,
            Subject,
            HtmlBody,
            TextBody,
            CorrelationId);

    public void MarkSent() {
        Status = QueuedEmailStatus.Sent;
        SetModified();
    }

    public void MarkSuppressed() {
        Status = QueuedEmailStatus.Suppressed;
        SetModified();
    }

    public QueuedEmailFailureDecision MarkFailedAttempt(string error) {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        bool isTerminalFailure = AttemptCount >= MaxAttempts;
        Status = isTerminalFailure ? QueuedEmailStatus.Failed : QueuedEmailStatus.Retry;
        SetModified();

        return new QueuedEmailFailureDecision(
            Id,
            AttemptCount,
            Status,
            isTerminalFailure,
            error);
    }
}
