namespace FoodDiary.MailInbox.Application.Messages.Models;

public sealed record InboundMailRetentionResult(int ContentPurgedCount, int MetadataDeletedCount);
