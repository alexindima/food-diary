using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminDashboardOverviewReadService {
    Task<Result<AdminDashboardOverviewModel>> GetAsync(DateOnly? fromDate, DateOnly? toDate, bool allTime, CancellationToken cancellationToken);
}
