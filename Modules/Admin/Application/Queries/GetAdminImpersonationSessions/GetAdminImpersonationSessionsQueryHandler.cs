using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetAdminImpersonationSessions;

public sealed class GetAdminImpersonationSessionsQueryHandler(IAdminImpersonationSessionReadRepository impersonationSessionRepository)
    : IQueryHandler<GetAdminImpersonationSessionsQuery, Result<PagedResponse<AdminImpersonationSessionReadModel>>> {
    public async Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> Handle(GetAdminImpersonationSessionsQuery query, CancellationToken cancellationToken) {
        int normalizedPage = PaginationPolicy.NormalizePage(query.Page);
        int normalizedLimit = PaginationPolicy.NormalizePageSizeOrDefault(query.Limit);
        (IReadOnlyList<AdminImpersonationSessionReadModel> items, int totalItems) =
            await impersonationSessionRepository.GetPagedAsync(normalizedPage, normalizedLimit, query.Search, cancellationToken, query.FromUtc, query.ToUtc, query.ActorId, query.TargetId).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(totalItems / (double)normalizedLimit);

        return Result.Success(new PagedResponse<AdminImpersonationSessionReadModel>(
            items,
            normalizedPage,
            normalizedLimit,
            totalPages,
            totalItems));
    }
}
