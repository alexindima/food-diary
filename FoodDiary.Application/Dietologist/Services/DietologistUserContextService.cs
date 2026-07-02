using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

internal sealed class DietologistUserContextService(IUserRepository userRepository) : IDietologistUserContextService {
    public async Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) {
        User? user = await userRepository.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        Error? accessError = CurrentUserAccessPolicy.EnsureCanAccess(user);
        return accessError is not null
            ? Result.Failure<User>(accessError)
            : Result.Success(user!);
    }

    public Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        userRepository.GetByEmailAsync(email, cancellationToken);

    public Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        userRepository.GetByIdAsync(userId, cancellationToken);
}
