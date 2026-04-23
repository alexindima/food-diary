namespace FoodDiary.MailRelay.Services;

public sealed record MailRelayFailureResult(
    bool IsTerminalFailure);
