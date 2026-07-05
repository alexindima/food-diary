using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminAuditReadService {
    Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> GetImpersonationSessionsAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> GetUserRoleAuditAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken);
}
