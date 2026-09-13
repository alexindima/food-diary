using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminDashboardMetricsReader {
    Task<AdminDashboardMetrics> GetAsync(DateTime fromUtc, DateTime toUtc, bool monthly, CancellationToken cancellationToken);
}
