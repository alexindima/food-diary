namespace FoodDiary.MailInbox.Presentation.Features.Export;

public sealed record InboundMailExportEntryHttpResponse(Guid Id, DateTimeOffset ReceivedAtUtc, bool ContentAvailable);
