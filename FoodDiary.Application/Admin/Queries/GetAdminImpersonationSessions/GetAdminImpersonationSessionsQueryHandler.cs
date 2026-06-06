using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed class GetAdminImpersonationSessionsQueryHandler(
    IAdminImpersonationSessionRepository repository)
    : IQueryHandler<GetAdminImpersonationSessionsQuery, Result<PagedResponse<AdminImpersonationSessionReadModel>>> {
    public async Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> Handle(
        GetAdminImpersonationSessionsQuery query,
        CancellationToken cancellationToken) {
        int page = query.Page <= 0 ? 1 : query.Page;
        int limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;
        (IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems) pageData = await repository.GetPagedAsync(page, limit, query.Search, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);

        return Result.Success(new PagedResponse<AdminImpersonationSessionReadModel>(
            pageData.Items,
            page,
            limit,
            totalPages,
            pageData.TotalItems));
    }
}
