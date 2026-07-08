using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Common.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminAuditReadService(
    IAdminUserReadService userReadService,
    IAdminUserRoleAuditReadRepository roleAuditRepository,
    IAdminImpersonationSessionReadRepository impersonationSessionRepository)
    : IAdminAuditReadService {
    public async Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> GetImpersonationSessionsAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken) {
        int normalizedPage = page <= 0 ? 1 : page;
        int normalizedLimit = limit is > 0 and <= 100 ? limit : 20;
        (IReadOnlyList<AdminImpersonationSessionReadModel> items, int totalItems) =
            await impersonationSessionRepository.GetPagedAsync(normalizedPage, normalizedLimit, search, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(totalItems / (double)normalizedLimit);

        return Result.Success(new PagedResponse<AdminImpersonationSessionReadModel>(
            items,
            normalizedPage,
            normalizedLimit,
            totalPages,
            totalItems));
    }

    public async Task<Result<IReadOnlyList<AdminUserRoleAuditEventReadModel>>> GetUserRoleAuditAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            userId,
            Errors.Validation.Invalid(nameof(userId), "User id must not be empty."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(userIdResult);
        }

        bool userExists = await userReadService.ExistsIncludingDeletedAsync(userIdResult.Value, cancellationToken).ConfigureAwait(false);
        if (!userExists) {
            return Result.Failure<IReadOnlyList<AdminUserRoleAuditEventReadModel>>(Errors.User.NotFound(userId));
        }

        int normalizedLimit = Math.Clamp(limit, 1, 50);
        IReadOnlyList<AdminUserRoleAuditEventReadModel> events =
            await roleAuditRepository.GetRecentForUserAsync(userId, normalizedLimit, cancellationToken).ConfigureAwait(false);

        return Result.Success(events);
    }
}
