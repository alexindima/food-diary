using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Application.Abstractions.Models;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.DailyAdvices.Contracts.Models;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;
using FoodDiary.Application.Tdee.Models;

namespace FoodDiary.Modules.Dashboard.Application.Services;

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
