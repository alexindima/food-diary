using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Abstractions.Users.Common;

public interface IUserRoleCatalogService {
    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
}
