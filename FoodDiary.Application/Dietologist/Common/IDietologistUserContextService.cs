using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistUserContextService : ICurrentUserAccessService {
    Task<Result<string>> GetAccessibleUserEmailAsync(UserId userId, CancellationToken cancellationToken);
    Task<string?> GetUserEmailByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<UserModel>> GetUserModelByIdAsync(UserId userId, CancellationToken cancellationToken);
    Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
    Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken);
}
