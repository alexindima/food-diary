namespace FoodDiary.MailRelay.Domain.Emails;

public sealed record QueuedEmailFailureDecision {
    public QueuedEmailId Id { get; }
    public int AttemptCount { get; }
    public QueuedEmailProcessingState State { get; }
    public bool IsTerminalFailure => State == QueuedEmailProcessingState.Failed;
    public string Error { get; }
    public string Status => QueuedEmailStateCodec.ToStorage(State);

    public QueuedEmailFailureDecision(QueuedEmailId id, int attemptCount, QueuedEmailProcessingState state, string error) {
        if (state is not (QueuedEmailProcessingState.Retry or QueuedEmailProcessingState.Failed)) {
            throw new ArgumentOutOfRangeException(nameof(state));
        }
        Id = id;
        AttemptCount = attemptCount;
        State = state;
        Error = error;
    }
}
