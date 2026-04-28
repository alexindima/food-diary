using FoodDiary.Domain.Entities.Admin;
using FoodDiary.Application.Abstractions.Admin.Models;

namespace FoodDiary.Application.Abstractions.Admin.Common;

public interface IAdminImpersonationSessionRepository {
    Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default);
}
