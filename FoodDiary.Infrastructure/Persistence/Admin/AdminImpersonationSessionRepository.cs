using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.Entities.Admin;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminImpersonationSessionRepository(FoodDiaryDbContext context) : IAdminImpersonationSessionRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task AddAsync(AdminImpersonationSession session, CancellationToken cancellationToken = default) {
        context.AdminImpersonationSessions.Add(session);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AdminImpersonationSessionReadModel> Items, int TotalItems)> GetPagedAsync(
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default) {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Clamp(limit, 1, 100);

        var query =
            from session in context.AdminImpersonationSessions.AsNoTracking()
            join actor in context.Users.AsNoTracking() on session.ActorUserId equals actor.Id
            join target in context.Users.AsNoTracking() on session.TargetUserId equals target.Id
            select new { session, actor, target };

        if (!string.IsNullOrWhiteSpace(search)) {
            var term = $"%{EscapeLikePattern(search)}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.actor.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.target.Email, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.session.Reason, term, LikeEscapeCharacter) ||
                EF.Functions.ILike(item.session.ActorIpAddress ?? string.Empty, term, LikeEscapeCharacter));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(item => item.session.StartedAtUtc)
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
            .ToListAsync(cancellationToken);

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
