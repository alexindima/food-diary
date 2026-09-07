using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Services;

public sealed class AdminDashboardOverviewReadService(IAdminDashboardMetricsReader metrics,
    IAdminBillingReadRepository billing, IAdminDashboardReadService dashboard, TimeProvider clock)
    : IAdminDashboardOverviewReadService {
    public async Task<Result<AdminDashboardOverviewModel>> GetAsync(DateOnly? fromDate, DateOnly? toDate, bool allTime, CancellationToken cancellationToken) {
        DateTime today = clock.GetUtcNow().UtcDateTime.Date;
        DateTime from = allTime ? DateTime.UnixEpoch : fromDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            ?? new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        // Both UI dates are inclusive; persistence uses one exclusive upper boundary.
        DateTime last = toDate?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) ?? today;
        if ((allTime && (fromDate.HasValue || toDate.HasValue)) || from < DateTime.UnixEpoch || last > today ||
            last < from || (!allTime && (last - from).TotalDays > 3660)) {
            return Result.Failure<AdminDashboardOverviewModel>(Errors.Validation.Invalid("period", "Choose a valid past period of at most ten years, or all time."));
        }
        DateTime to = last.AddDays(1);
        bool monthly = (to - from).TotalDays > 90;
        AdminDashboardPeriodModel period = await ReadPeriodAsync(from, to, monthly, cancellationToken).ConfigureAwait(false);
        AdminDashboardPeriodModel? previous = allTime ? null :
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
