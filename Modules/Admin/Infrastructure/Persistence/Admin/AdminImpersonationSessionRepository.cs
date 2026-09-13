using FoodDiary.Modules.Admin.Application.Abstractions.Common;
using FoodDiary.Modules.Admin.Application.Abstractions.Models;
using FoodDiary.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminImpersonationSessionRepository(
    DbSet<AdminImpersonationSession> sessions, IAdminImpersonationSessionQuery queries) : IAdminImpersonationSessionRepository {
    public async Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default) {
        await sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page, int limit, string? search, CancellationToken cancellationToken = default,
        DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? actorId = null, Guid? targetId = null) =>
        queries.GetPagedAsync(page, limit, search, cancellationToken, fromUtc, toUtc, actorId, targetId);
}
