using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminImpersonationSessionRepository(FoodDiaryDbContext context) : IAdminImpersonationSessionRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default) {
        await context.AdminImpersonationSessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, Guid? actorId = null, Guid? targetId = null) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);

        var query =
            from session in context.AdminImpersonationSessions.AsNoTracking()
            join actor in context.Users.AsNoTracking() on session.ActorUserId equals actor.Id
            join target in context.Users.AsNoTracking() on session.TargetUserId equals target.Id
            select new { session, actor, target };

        if (!string.IsNullOrWhiteSpace(search)) {
            string term = $"%{EscapeLikePattern(search)}%";
            query = query.Where(item =>
                (item.actor.Email != null && EF.Functions.ILike(item.actor.Email, term, LikeEscapeCharacter)) ||
                (item.target.Email != null && EF.Functions.ILike(item.target.Email, term, LikeEscapeCharacter)) ||
                EF.Functions.ILike(item.session.Reason, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.session.ActorIpAddress ?? string.Empty, term, LikeEscapeCharacter));
        }

        if (fromUtc.HasValue) { DateTime start = fromUtc.Value.UtcDateTime; query = query.Where(item => item.session.StartedAtUtc >= start); }
        if (toUtc.HasValue) { DateTime end = toUtc.Value.UtcDateTime; query = query.Where(item => item.session.StartedAtUtc < end); }
        if (actorId.HasValue) { var actor = new FoodDiary.Domain.ValueObjects.Ids.UserId(actorId.Value); query = query.Where(item => item.session.ActorUserId == actor); }
        if (targetId.HasValue) { var target = new FoodDiary.Domain.ValueObjects.Ids.UserId(targetId.Value); query = query.Where(item => item.session.TargetUserId == target); }
        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<AdminImpersonationSessionReadModel> items = await query
            .OrderByDescending(item => item.session.StartedAtUtc)
            .ThenBy(item => item.session.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new AdminImpersonationSessionReadModel(
                item.session.Id,
                item.actor.Id.Value,
                item.actor.Email,
                item.target.Id.Value,
                item.target.Email,
                item.session.Reason,
                item.session.ActorIpAddress,
                item.session.ActorUserAgent,
                item.session.StartedAtUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
