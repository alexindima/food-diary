namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailExportEntry(Guid Id, DateTimeOffset ReceivedAtUtc, bool ContentAvailable);
