using FoodDiary.MailRelay.Domain.Common;

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
    public int AttemptCount { get; private set; }
    public int MaxAttempts { get; }
    public string Status { get; private set; }

    public static QueuedEmail FromPersistence(QueuedEmailMessage message) =>
        new(
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
    }

    public void MarkSuppressed() {
        Status = QueuedEmailStatus.Suppressed;
    }

    public QueuedEmailFailureDecision MarkFailedAttempt(string error) {
        ArgumentException.ThrowIfNullOrWhiteSpace(error);

        var isTerminalFailure = AttemptCount >= MaxAttempts;
        Status = isTerminalFailure ? QueuedEmailStatus.Failed : QueuedEmailStatus.Retry;

        return new QueuedEmailFailureDecision(
            Id,
            AttemptCount,
            Status,
            isTerminalFailure,
            error);
    }
}
