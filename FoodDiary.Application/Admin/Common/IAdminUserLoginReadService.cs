using FoodDiary.Results;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Models;

namespace FoodDiary.Application.Admin.Common;

public interface IAdminUserLoginReadService {
    Task<Result<PagedResponse<AdminUserLoginEventModel>>> GetEventsAsync(
        int page,
        int limit,
        Guid? userId,
        string? search,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<AdminUserLoginDeviceSummaryModel>>> GetSummaryAsync(
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken cancellationToken);
}
