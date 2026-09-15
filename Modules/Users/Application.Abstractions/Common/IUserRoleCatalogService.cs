using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Abstractions.Common;

public interface IUserRoleCatalogService {
    Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> EnsureRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default);
}
