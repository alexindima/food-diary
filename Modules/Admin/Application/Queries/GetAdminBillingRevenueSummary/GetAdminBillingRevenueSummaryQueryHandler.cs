using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingRevenueSummary;

public sealed class GetAdminBillingRevenueSummaryQueryHandler(IAdminBillingReadService readService)
    : IQueryHandler<GetAdminBillingRevenueSummaryQuery, Result<AdminBillingRevenueSummaryReadModel>> {
    public Task<Result<AdminBillingRevenueSummaryReadModel>> Handle(
        GetAdminBillingRevenueSummaryQuery query,
        CancellationToken cancellationToken) =>
        readService.GetRevenueSummaryAsync(query.FromUtc, query.ToUtc, cancellationToken);
}
