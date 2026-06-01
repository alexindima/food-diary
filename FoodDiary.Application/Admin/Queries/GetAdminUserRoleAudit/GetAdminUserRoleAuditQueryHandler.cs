using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;

public sealed class GetAdminUserRoleAuditQueryHandler(
    IUserRepository userRepository,
    IAdminUserRoleAuditRepository roleAuditRepository)
    : IQueryHandler<GetAdminUserRoleAuditQuery, Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> Handle(
        GetAdminUserRoleAuditQuery query,
        CancellationToken cancellationToken) {
        if (query.UserId == Guid.Empty) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(
                Errors.Validation.Invalid(nameof(query.UserId), "User id must not be empty."));
        }

        var user = await userRepository.GetByIdIncludingDeletedAsync(new UserId(query.UserId), cancellationToken).ConfigureAwait(false);
        if (user is null) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(Errors.User.NotFound(query.UserId));
        }

        var limit = Math.Clamp(query.Limit, 1, 50);
        var events = await roleAuditRepository.GetRecentForUserAsync(query.UserId, limit, cancellationToken).ConfigureAwait(false);
        return Result.Success(events);
    }
}
