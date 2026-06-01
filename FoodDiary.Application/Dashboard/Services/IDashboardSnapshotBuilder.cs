using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Dashboard.Services;

public interface IDashboardSnapshotBuilder {
    Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default);
}
