using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminImpersonationUserService {
    Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default);
}
