namespace FoodDiary.MailRelay.Application.Queue.Models;

public union MailRelayProcessResult(
    MailRelayProcessResult.Sent,
    MailRelayProcessResult.TerminalFailure,
    MailRelayProcessResult.Retry,
    MailRelayProcessResult.Recoverable) {
    public sealed class Sent;
    public sealed class TerminalFailure;
    public sealed record Retry(DateTimeOffset? AvailableAtUtc);
    public sealed class Recoverable;

    public bool Succeeded => this is Sent;
    public bool IsTerminalFailure => this is TerminalFailure;
    public DateTimeOffset? RetryAvailableAtUtc => this is Retry retry ? retry.AvailableAtUtc : null;
}
