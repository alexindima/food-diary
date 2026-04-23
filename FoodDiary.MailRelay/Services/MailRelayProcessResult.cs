namespace FoodDiary.MailRelay.Services;

public sealed record MailRelayProcessResult(
    bool Succeeded,
    bool IsTerminalFailure);
