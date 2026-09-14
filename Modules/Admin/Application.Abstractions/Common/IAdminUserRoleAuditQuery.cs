using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminUserRoleAuditQuery {
    Task<IReadOnlyList<AdminUserRoleAuditEventReadModel>> GetRecentForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);
}
