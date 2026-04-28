using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed class GetAdminImpersonationSessionsQueryHandler(
    IAdminImpersonationSessionRepository repository)
    : IQueryHandler<GetAdminImpersonationSessionsQuery, Result<PagedResponse<AdminImpersonationSessionReadModel>>> {
    public async Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> Handle(
        GetAdminImpersonationSessionsQuery query,
        CancellationToken cancellationToken) {
        var page = query.Page <= 0 ? 1 : query.Page;
        var limit = query.Limit is > 0 and <= 100 ? query.Limit : 20;
        var pageData = await repository.GetPagedAsync(page, limit, query.Search, cancellationToken);
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)limit);

        return Result.Success(new PagedResponse<AdminImpersonationSessionReadModel>(
            pageData.Items,
            page,
            limit,
            totalPages,
            pageData.TotalItems));
    }
}
