using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramOidcProvider {
    bool IsEnabled { get; }
    Result<string> CreateAuthorizationUrl(string state, string nonce, string codeVerifier);
    Task<Result<TelegramOidcIdentity>> ExchangeAsync(string code, string codeVerifier, string nonce, CancellationToken cancellationToken);
}
