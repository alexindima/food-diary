using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.Abstractions.Authentication.Abstractions;

public interface IGoogleTokenValidator {
    Task<Result<GoogleIdentityPayload>> ValidateCredentialAsync(string credential, CancellationToken cancellationToken);
}
