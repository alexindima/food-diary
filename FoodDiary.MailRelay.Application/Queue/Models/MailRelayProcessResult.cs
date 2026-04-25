namespace FoodDiary.MailRelay.Application.Queue.Models;

public sealed record MailRelayProcessResult(
    bool Succeeded,
    bool IsTerminalFailure);
