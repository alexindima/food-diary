using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Admin;

public sealed class AdminUserRoleAuditRepository(FoodDiaryDbContext context) : IAdminUserRoleAuditRepository {
    public async Task<IReadOnlyList<AdminUserRoleAuditEventReadModel>> GetRecentForUserAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default) {
        var userIdValue = new UserId(userId);
        var pageSize = Math.Clamp(limit, 1, 50);

        return await (
                from auditEvent in context.UserRoleAuditEvents.AsNoTracking()
                where auditEvent.UserId == userIdValue
                join actor in context.Users.AsNoTracking() on auditEvent.ActorUserId equals actor.Id into actors
                from actor in actors.DefaultIfEmpty()
                orderby auditEvent.OccurredAtUtc descending
                select new AdminUserRoleAuditEventReadModel(
                    auditEvent.Id,
                    auditEvent.UserId.Value,
                    auditEvent.RoleName,
                    auditEvent.Action.ToString(),
                    auditEvent.ActorUserId.HasValue ? auditEvent.ActorUserId.Value.Value : null,
                    actor != null ? actor.Email : null,
                    auditEvent.Source,
                    auditEvent.OccurredAtUtc))
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
