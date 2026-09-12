using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Admin;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminImpersonationSessionRepository(
    FoodDiaryDbContext context, IAdminImpersonationSessionQuery queries) : IAdminImpersonationSessionRepository {
    public async Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default) {
        await context.AdminImpersonationSessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page, int limit, string? search, CancellationToken cancellationToken = default,
        DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? actorId = null, Guid? targetId = null) =>
        queries.GetPagedAsync(page, limit, search, cancellationToken, fromUtc, toUtc, actorId, targetId);
}
