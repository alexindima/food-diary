using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminUserRoleAuditRepository {
    Task<IReadOnlyList<AdminUserRoleAuditEventReadModel>> GetRecentForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);
}
