using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistUserContextService {
    Task<Result<User>> GetAccessibleUserAsync(UserId userId, CancellationToken cancellationToken);
    Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken);
}
