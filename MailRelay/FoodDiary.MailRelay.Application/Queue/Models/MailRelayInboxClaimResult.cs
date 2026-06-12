namespace FoodDiary.MailRelay.Application.Queue.Models;

public sealed record MailRelayInboxClaimResult(
    bool Claimed,
    Guid InboxId);
