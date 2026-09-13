using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminDashboardOverview;

public sealed class GetAdminDashboardOverviewQueryHandler(IAdminDashboardMetricsReader metrics, IAdminBillingReadRepository billing, IAdminDashboardReadService dashboard, TimeProvider clock)
    : IQueryHandler<GetAdminDashboardOverviewQuery, Result<AdminDashboardOverviewModel>> {
    public async Task<Result<AdminDashboardOverviewModel>> Handle(GetAdminDashboardOverviewQuery query, CancellationToken cancellationToken) {
        DateTime today = clock.GetUtcNow().UtcDateTime.Date;
        DateTime from = query.AllTime ? DateTime.UnixEpoch : query.From?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            ?? new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        // Both UI dates are inclusive; persistence uses one exclusive upper boundary.
        DateTime last = query.To?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? today;
        if ((query.AllTime && (query.From.HasValue || query.To.HasValue)) || from < DateTime.UnixEpoch || last > today ||
            last < from || (!query.AllTime && (last - from).TotalDays > 3660)) {
            return Result.Failure<AdminDashboardOverviewModel>(Errors.Validation.Invalid("period", "Choose a valid past period of at most ten years, or all time."));
        }
        DateTime to = last.AddDays(1);
        bool monthly = (to - from).TotalDays > 90;
        AdminDashboardPeriodModel period = await ReadPeriodAsync(from, to, monthly, cancellationToken).ConfigureAwait(false);
        AdminDashboardPeriodModel? previous = query.AllTime ? null :
            await ReadPeriodAsync(from - (to - from), from, monthly, cancellationToken).ConfigureAwait(false);
        Result<AdminDashboardSummaryModel> current = await dashboard.GetSummaryAsync(1, cancellationToken).ConfigureAwait(false);
        if (current.IsFailure) {
            return Result.Failure<AdminDashboardOverviewModel>(current.Error);
        }
        return Result.Success(new AdminDashboardOverviewModel(from, to, monthly ? "month" : "day", period, previous,
            current.Value.TotalUsers, current.Value.PremiumUsers, current.Value.PendingReportsCount));
    }
    private async Task<AdminDashboardPeriodModel> ReadPeriodAsync(DateTime from, DateTime to, bool monthly, CancellationToken cancellationToken) {
        AdminDashboardMetrics counts = await metrics.GetAsync(from, to, monthly, cancellationToken).ConfigureAwait(false);
        AdminBillingRevenueSummaryReadModel revenue = await billing.GetRevenueSummaryAsync(from, to, cancellationToken).ConfigureAwait(false);
        return new AdminDashboardPeriodModel(from, to, counts, revenue.Currencies);
    }
}
