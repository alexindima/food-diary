using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;

public sealed class GetAdminUserRoleAuditQueryHandler(IAdminAuditReadService readService)
    : IQueryHandler<GetAdminUserRoleAuditQuery, Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> Handle(
        GetAdminUserRoleAuditQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetUserRoleAuditAsync(query.UserId, query.Limit, cancellationToken).ConfigureAwait(false);
    }
}
