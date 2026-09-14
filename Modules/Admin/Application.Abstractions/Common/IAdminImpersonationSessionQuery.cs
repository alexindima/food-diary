using FoodDiary.Modules.Admin.Application.Abstractions.Models;

namespace FoodDiary.Modules.Admin.Application.Abstractions.Common;

public interface IAdminImpersonationSessionQuery {
    Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? actorId = null, Guid? targetId = null);
}
