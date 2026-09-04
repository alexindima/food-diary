using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Users.Services;

internal sealed class UserCredentialVerificationService(
    IUserLookupRepository userLookupRepository,
    IPasswordHasher passwordHasher) : IUserCredentialVerificationService {
    public async Task<Result> VerifyPasswordAsync(
        UserId userId,
        string password,
        CancellationToken cancellationToken = default) {
        User? user = await userLookupRepository
            .GetByIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        if (user is null) {
            return Result.Failure(UserErrors.NotFound(userId));
        }

        if (!user.HasPassword) {
            return Result.Failure(UserErrors.PasswordNotSet);
        }

        return passwordHasher.Verify(password, user.Password)
            ? Result.Success()
            : Result.Failure(UserErrors.InvalidPassword);
    }
}
