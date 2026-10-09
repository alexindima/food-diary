namespace FoodDiary.MailRelay.Domain.Emails;

public static class QueuedEmailStateCodec {
    public static QueuedEmailProcessingState FromStorage(string value) => value switch {
        QueuedEmailStatus.Pending => QueuedEmailProcessingState.Pending,
        QueuedEmailStatus.Retry => QueuedEmailProcessingState.Retry,
        QueuedEmailStatus.Processing => QueuedEmailProcessingState.Processing,
        QueuedEmailStatus.Sent => QueuedEmailProcessingState.Sent,
        QueuedEmailStatus.Failed => QueuedEmailProcessingState.Failed,
        QueuedEmailStatus.Suppressed => QueuedEmailProcessingState.Suppressed,
        _ => QueuedEmailProcessingState.Unrecognized,
    };

    public static string ToStorage(QueuedEmailProcessingState state) => state switch {
        QueuedEmailProcessingState.Pending => QueuedEmailStatus.Pending,
        QueuedEmailProcessingState.Retry => QueuedEmailStatus.Retry,
        QueuedEmailProcessingState.Processing => QueuedEmailStatus.Processing,
        QueuedEmailProcessingState.Sent => QueuedEmailStatus.Sent,
        QueuedEmailProcessingState.Failed => QueuedEmailStatus.Failed,
        QueuedEmailProcessingState.Suppressed => QueuedEmailStatus.Suppressed,
        _ => throw new ArgumentOutOfRangeException(nameof(state), "Unrecognized states have no canonical storage code."),
    };
}
