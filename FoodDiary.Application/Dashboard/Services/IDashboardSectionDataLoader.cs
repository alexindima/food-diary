using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Domain.Entities.Tracking.Fasting;

namespace FoodDiary.Application.Dashboard.Services;

internal interface IDashboardSectionDataLoader {
    Task<Result<DashboardBuildContext>> CreateBuildContextAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken);

    Task<Result<DashboardStatisticsSection>> LoadStatisticsAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<Result<DashboardMealsModel>> LoadMealsAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<DashboardBodySection> LoadBodyAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<Result<DailyAdviceModel>?> LoadAdviceAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<FastingOccurrence?> LoadFastingAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<double> LoadCaloriesBurnedAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<Result<TdeeInsightModel>?> LoadTdeeAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<Result<CycleModel?>?> LoadCycleAsync(
        DashboardSnapshotRequest request,
        DashboardBuildContext context,
        CancellationToken cancellationToken);
}
