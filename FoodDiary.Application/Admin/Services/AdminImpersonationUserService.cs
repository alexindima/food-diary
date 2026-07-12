using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

internal sealed class AdminImpersonationUserService(IUserDirectoryService userRepository) : IAdminImpersonationUserService {
    public Task<User?> GetByIdAsync(UserId userId, CancellationToken cancellationToken = default) =>
        userRepository.GetByIdAsync(userId, cancellationToken);
}
