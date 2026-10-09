using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramOidcProvider {
    bool IsEnabled { get; }
    Result<string> CreateAuthorizationUrl(TelegramOidcAuthorizationRequest request);
    Task<Result<TelegramOidcIdentity>> ExchangeAsync(TelegramOidcTokenExchange exchange, CancellationToken cancellationToken);
}
