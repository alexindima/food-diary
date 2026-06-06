using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Dashboard.Models;

namespace FoodDiary.Application.Dashboard.Services;

public interface IDashboardSnapshotBuilder {
    Task<Result<DashboardSnapshotModel>> BuildAsync(
        DashboardSnapshotRequest request,
        CancellationToken cancellationToken = default);
}
