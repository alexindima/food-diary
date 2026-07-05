using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Admin.Queries.GetAdminImpersonationSessions;

public sealed class GetAdminImpersonationSessionsQueryHandler(IAdminAuditReadService readService)
    : IQueryHandler<GetAdminImpersonationSessionsQuery, Result<PagedResponse<AdminImpersonationSessionReadModel>>> {
    public async Task<Result<PagedResponse<AdminImpersonationSessionReadModel>>> Handle(
        GetAdminImpersonationSessionsQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetImpersonationSessionsAsync(
            query.Page,
            query.Limit,
            query.Search,
            cancellationToken).ConfigureAwait(false);
    }
}
