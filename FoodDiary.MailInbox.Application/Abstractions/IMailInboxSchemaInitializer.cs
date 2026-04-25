namespace FoodDiary.MailInbox.Application.Abstractions;

public interface IMailInboxSchemaInitializer {
    Task EnsureSchemaAsync(CancellationToken cancellationToken);
}
