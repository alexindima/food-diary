using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

internal sealed class DietologistUserLookupService(IUserReadRepository userRepository) : IDietologistUserLookupService {
    public Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        userRepository.GetByEmailAsync(email, cancellationToken);

    public Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        userRepository.GetByIdAsync(userId, cancellationToken);
}
