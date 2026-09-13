using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminUserRoleAuditReadRepository {
    Task<IReadOnlyList<AdminUserRoleAuditEventReadModel>> GetRecentForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);
}
