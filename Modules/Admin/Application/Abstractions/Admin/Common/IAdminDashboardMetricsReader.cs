using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminDashboardMetricsReader {
    Task<AdminDashboardMetrics> GetAsync(DateTime fromUtc, DateTime toUtc, bool monthly, CancellationToken cancellationToken);
}
