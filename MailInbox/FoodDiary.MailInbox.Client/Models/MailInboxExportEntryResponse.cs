namespace FoodDiary.MailInbox.Client.Models;

public sealed record MailInboxExportEntryResponse(Guid Id, DateTimeOffset ReceivedAtUtc, bool ContentAvailable);
