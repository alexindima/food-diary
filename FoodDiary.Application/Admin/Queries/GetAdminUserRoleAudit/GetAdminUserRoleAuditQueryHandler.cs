using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;

public sealed class GetAdminUserRoleAuditQueryHandler(
    IAdminUserReadService userReadService,
    IAdminUserRoleAuditReadRepository roleAuditRepository)
    : IQueryHandler<GetAdminUserRoleAuditQuery, Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> Handle(
        GetAdminUserRoleAuditQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        User? user = await userReadService.GetByIdIncludingDeletedAsync(new UserId(query.UserId), cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(Errors.User.NotFound(query.UserId));
        }

        int limit = Math.Clamp(query.Limit, 1, 50);
        IReadOnlyList<AdminUserRoleAuditEventReadModel> events = await roleAuditRepository.GetRecentForUserAsync(query.UserId, limit, cancellationToken).ConfigureAwait(false);
        return Result.Success(events);
    }
}
