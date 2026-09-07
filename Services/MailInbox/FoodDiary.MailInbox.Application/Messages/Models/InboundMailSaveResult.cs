namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailSaveResult(Guid Id, bool WasDuplicate);
