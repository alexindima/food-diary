using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
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
            return Result.Failure(Errors.User.NotFound(userId));
        }

        if (!user.HasPassword) {
            return Result.Failure(Errors.User.PasswordNotSet);
        }

        return passwordHasher.Verify(password, user.Password)
            ? Result.Success()
            : Result.Failure(Errors.User.InvalidPassword);
    }
}
