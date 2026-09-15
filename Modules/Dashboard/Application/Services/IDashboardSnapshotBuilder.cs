using FoodDiary.Results;
using FoodDiary.Modules.Dashboard.Contracts.Models;

namespace FoodDiary.Modules.Dashboard.Application.Services;

public interface IDashboardSnapshotBuilder {
    Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default);
}
