using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminDashboardReadService {
    Task<Result<AdminDashboardSummaryModel>> GetSummaryAsync(int recentLimit, CancellationToken cancellationToken);
}
