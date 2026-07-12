using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Users.Common;

public interface IUserIdentityMutationService {
    Task<User> AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(
        IReadOnlyList<string> names,
        CancellationToken cancellationToken = default);
}
