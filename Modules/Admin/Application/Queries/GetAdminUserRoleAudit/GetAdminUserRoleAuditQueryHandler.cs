using FoodDiary.Mediator;
using FoodDiary.Application.Abstractions.Users.Queries.GetUserForAdministration;
using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminUserRoleAudit;

public sealed class GetAdminUserRoleAuditQueryHandler(ISender userReadService, IAdminUserRoleAuditQuery roleAuditRepository)
    : IQueryHandler<GetAdminUserRoleAuditQuery, Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> {
    public async Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> Handle(GetAdminUserRoleAuditQuery query, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            query.UserId,
            Errors.Validation.Invalid("userId", "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(userIdResult);
        }

        bool userExists = await userReadService.Send(new GetUserForAdministrationQuery(UserId: userIdResult.Value), cancellationToken).ConfigureAwait(false) is not null;
        if (!userExists) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(UserErrors.NotFound(query.UserId));
        }

        int normalizedLimit = Math.Clamp(query.Limit, 1, 50);
        IReadOnlyList<AdminUserRoleAuditEventReadModel> events =
            await roleAuditRepository.GetRecentForUserAsync(query.UserId, normalizedLimit, cancellationToken).ConfigureAwait(false);

        return Result.Success(events);
    }
}
