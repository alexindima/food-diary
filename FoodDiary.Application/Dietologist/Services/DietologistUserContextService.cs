using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dietologist.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Services;

internal sealed class DietologistUserContextService(
    IUserContextService userContextService,
    IDietologistUserLookupService userLookupService) : IDietologistUserContextService {
    public Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken) =>
        userContextService.GetAccessibleUserAsync(userId, cancellationToken);

    public Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken) =>
        userLookupService.GetAccessibleUserByEmailAsync(email, cancellationToken);

    public Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken) =>
        userLookupService.GetUserByIdAsync(userId, cancellationToken);
}
