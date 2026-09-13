using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;

public sealed class GetAdminUserRoleAuditQueryHandler(IAdminUserReadService userReadService, IAdminUserRoleAuditReadRepository roleAuditRepository)
    : IQueryHandler<GetAdminUserRoleAuditQuery, Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> Handle(GetAdminUserRoleAuditQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            query.UserId,
            Errors.Validation.Invalid("userId", "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(userIdResult);
        }

        bool userExists = await userReadService.ExistsIncludingDeletedAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        if (!userExists) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(UserErrors.NotFound(query.UserId));
        }

        int normalizedLimit = Math.Clamp(query.Limit, 1, 50);
        IReadOnlyList<AdminUserRoleAuditEventReadModel> events =
            await roleAuditRepository.GetRecentForUserAsync(query.UserId, normalizedLimit, cancellationToken).ConfigureAwait(false);

        return Result.Success(events);
    }
}
