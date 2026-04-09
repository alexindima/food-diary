using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Authentication.Abstractions;

public interface IGoogleTokenValidator {
    Task<Result<GoogleIdentityPayload>> ValidateCredentialAsync(string credential, CancellationToken cancellationToken);
}
