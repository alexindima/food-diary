using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminAiUsageReadService {
    Task<Result<AdminAiUsageSummaryModel>> GetSummaryAsync(
        DateOnly? from,
        DateOnly? to,
        CancellationToken cancellationToken);
}
