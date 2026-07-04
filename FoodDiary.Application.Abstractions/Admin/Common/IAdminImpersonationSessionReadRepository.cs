using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminImpersonationSessionReadRepository {
    Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default);
}
