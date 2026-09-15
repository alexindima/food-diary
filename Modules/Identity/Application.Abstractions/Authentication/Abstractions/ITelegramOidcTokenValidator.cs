using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface ITelegramOidcTokenValidator {
    Task<Result<TelegramOidcIdentity>> ValidateAsync(string idToken, string expectedNonce, CancellationToken cancellationToken);
}
