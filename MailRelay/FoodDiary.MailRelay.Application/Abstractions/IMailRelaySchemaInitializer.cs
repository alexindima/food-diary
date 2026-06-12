namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelaySchemaInitializer {
    Task EnsureSchemaAsync(CancellationToken cancellationToken);
}
