using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Dietologist.Common;

public interface IDietologistUserLookupService {
    Task<User?> GetAccessibleUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetUserByIdAsync(UserId userId, CancellationToken cancellationToken);
}
