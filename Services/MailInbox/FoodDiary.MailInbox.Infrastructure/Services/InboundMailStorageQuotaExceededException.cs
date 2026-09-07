namespace FoodDiary.MailInbox.Infrastructure.Services;

internal sealed class InboundMailStorageQuotaExceededException()
    : Exception("MailInbox daily ingestion storage quota is exhausted.");
