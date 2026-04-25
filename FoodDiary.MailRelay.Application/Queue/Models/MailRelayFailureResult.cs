namespace FoodDiary.MailRelay.Application.Queue.Models;

public sealed record MailRelayFailureResult(
    bool IsTerminalFailure);
