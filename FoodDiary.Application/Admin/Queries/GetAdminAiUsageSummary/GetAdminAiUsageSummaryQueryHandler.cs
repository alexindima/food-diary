using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;

public sealed class GetAdminAiUsageSummaryQueryHandler(IAdminAiUsageReadService readService)
    : IQueryHandler<GetAdminAiUsageSummaryQuery, Result<AdminAiUsageSummaryModel>> {
    public async Task<Result<AdminAiUsageSummaryModel>> Handle(
        GetAdminAiUsageSummaryQuery query,
        CancellationToken cancellationToken) {
        return await readService.GetSummaryAsync(query.From, query.To, cancellationToken).ConfigureAwait(false);
    }
}
