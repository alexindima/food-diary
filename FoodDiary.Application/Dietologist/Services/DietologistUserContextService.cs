using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

internal sealed class DietologistUserContextService(
    IUserContextService userContextService,
    IDietologistUserLookupService userLookupService) : IDietologistUserContextService {
    public async Task<Result<string>> GetAccessibleUserEmailAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        Result<User> userResult = await userContextService.GetAccessibleUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return userResult.IsFailure
            ? Result.Failure<string>(userResult.Error)
            : Result.Success(userResult.Value.Email);
    }

    public async Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userLookupService.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user?.Email;
    }

    public async Task<Result<UserModel>> GetUserModelByIdAsync(
        UserId userId,
        CancellationToken cancellationToken) {
        User? user = await userLookupService.GetUserByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return user is null
            ? Result.Failure<UserModel>(Errors.Dietologist.AccessDenied)
            : Result.Success(user.ToModel());
    }

    public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userContextService.GetAccessibleUserAsync(userId, cancellationToken);

    public Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        userLookupService.GetAccessibleUserByEmailAsync(email, cancellationToken);

    public Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        userLookupService.GetUserByIdAsync(userId, cancellationToken);
}
