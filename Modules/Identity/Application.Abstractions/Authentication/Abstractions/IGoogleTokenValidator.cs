using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

public interface IGoogleTokenValidator {
    Task<Result<GoogleIdentityPayload>> ValidateCredentialAsync(string credential, CancellationToken cancellationToken);
}
