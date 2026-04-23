namespace FoodDiary.MailRelay.Services;

public sealed record MailRelayInboxClaimResult(
    bool Claimed,
    Guid InboxId);
