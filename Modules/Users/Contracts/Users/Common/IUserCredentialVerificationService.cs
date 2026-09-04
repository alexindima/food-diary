using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserCredentialVerificationService {
    Task<Result> VerifyPasswordAsync(
        UserId userId,
        string password,
        CancellationToken cancellationToken = default);
}
