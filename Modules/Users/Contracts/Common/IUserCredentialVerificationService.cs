using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Contracts.Common;

public interface IUserCredentialVerificationService {
    Task<Result> VerifyPasswordAsync(
        UserId userId,
        string password,
        CancellationToken cancellationToken = default);
}
