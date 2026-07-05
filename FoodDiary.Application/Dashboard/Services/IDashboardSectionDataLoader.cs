using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Fasting.Models;
using FoodDiary.Application.Tdee.Models;

namespace FoodDiary.Application.Dashboard.Services;

internal interface IDashboardSectionDataLoader {
    Task<Result<DashboardBuildContext>> CreateBuildContextAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken);

    Task<Result<DashboardReadModel>> LoadDashboardDataAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<Result<DailyAdviceModel>?> LoadAdviceAsync(
        DashboardBuildContext context,
        CancellationToken cancellationToken);

    Task<FastingSessionModel?> LoadFastingAsync(
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
