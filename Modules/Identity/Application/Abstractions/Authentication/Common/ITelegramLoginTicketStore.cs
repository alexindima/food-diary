namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface ITelegramLoginTicketStore {
    Task<string> CreateAsync(string purpose, string browserBinding, string payload, DateTime expiresAtUtc, CancellationToken cancellationToken);
    Task<string?> ConsumeAsync(string ticket, string purpose, string browserBinding, CancellationToken cancellationToken);
}
