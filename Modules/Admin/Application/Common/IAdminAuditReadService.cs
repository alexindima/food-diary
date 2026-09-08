using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminAuditReadService {
    Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> GetImpersonationSessionsAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? actorId = null, Guid? targetId = null);

    Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> GetUserRoleAuditAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken);
}
