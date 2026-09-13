using FoodDiary.Results;
using FoodDiary.Modules.Admin.Application.Models;

namespace FoodDiary.Modules.Admin.Application.Common;

public interface IAdminDashboardReadService {
    Task<Result<AdminDashboardSummaryModel>> GetSummaryAsync(int recentLimit, CancellationToken cancellationToken);
}
