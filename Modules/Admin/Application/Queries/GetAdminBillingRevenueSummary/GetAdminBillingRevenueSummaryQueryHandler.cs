using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminBillingRevenueSummary;

public sealed class GetAdminBillingRevenueSummaryQueryHandler(IAdminBillingReadRepository billingRepository, TimeProvider? timeProvider = null)
    : IQueryHandler<GetAdminBillingRevenueSummaryQuery, Result<AdminBillingRevenueSummaryReadModel>> {
    public async Task<Result<AdminBillingRevenueSummaryReadModel>> Handle(GetAdminBillingRevenueSummaryQuery query, CancellationToken cancellationToken) {
        DateTime nowUtc = (timeProvider ?? TimeProvider.System).GetUtcNow().UtcDateTime;
        DateTime normalizedFrom = query.FromUtc?.ToUniversalTime() ?? new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime normalizedTo = query.ToUtc?.ToUniversalTime() ?? normalizedFrom.AddMonths(1);
        if (normalizedFrom >= normalizedTo) {
            return Result.Failure<AdminBillingRevenueSummaryReadModel>(
                Errors.Validation.Invalid("fromUtc", "Billing revenue range must have FromUtc before ToUtc."));
        }

        AdminBillingRevenueSummaryReadModel summary = await billingRepository
            .GetRevenueSummaryAsync(normalizedFrom, normalizedTo, cancellationToken)
            .ConfigureAwait(false);
        return Result.Success(summary);
    }
}
