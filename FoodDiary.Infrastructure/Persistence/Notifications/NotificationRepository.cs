using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

public class NotificationRepository(FoodDiaryDbContext context) : INotificationRepository {
    public async Task<IReadOnlyList<Notification>> GetByUserAsync(
        UserId userId, int limit = 50, CancellationToken cancellationToken = default) {
        return await context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedOnUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<Notification?> GetByIdAsync(
        NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<Notification> query = context.Notifications;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default) {
        context.Notifications.Add(notification);
        await context.SaveChangesAsync(cancellationToken);
        return notification;
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) {
        context.Notifications.Update(notification);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        UserId userId,
        string type,
        string referenceId,
        CancellationToken cancellationToken = default) {
        return await context.Notifications
            .AsNoTracking()
            .AnyAsync(
                n => n.UserId == userId &&
                    n.Type == type &&
                    n.ReferenceId == referenceId,
                cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default) {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead && n.Type == type, cancellationToken);
    }

    public async Task MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default) {
        await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAtUtc, DateTime.UtcNow)
                .SetProperty(n => n.ModifiedOnUtc, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task<int> DeleteExpiredBatchAsync(
        IReadOnlyCollection<string> transientTypes,
        DateTime transientReadOlderThanUtc,
        DateTime transientUnreadOlderThanUtc,
        DateTime standardReadOlderThanUtc,
        DateTime standardUnreadOlderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        if (batchSize <= 0) {
            return 0;
        }

        var transientTypeList = transientTypes
            .Where(static type => !string.IsNullOrWhiteSpace(type))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var candidates = await context.Notifications
            .Where(n =>
                (transientTypeList.Contains(n.Type) &&
                 ((n.IsRead && n.CreatedOnUtc < transientReadOlderThanUtc) ||
                  (!n.IsRead && n.CreatedOnUtc < transientUnreadOlderThanUtc))) ||
                (!transientTypeList.Contains(n.Type) &&
                 ((n.IsRead && n.CreatedOnUtc < standardReadOlderThanUtc) ||
                  (!n.IsRead && n.CreatedOnUtc < standardUnreadOlderThanUtc))))
            .OrderBy(n => n.CreatedOnUtc)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0) {
            return 0;
        }

        context.Notifications.RemoveRange(candidates);
        await context.SaveChangesAsync(cancellationToken);
        return candidates.Count;
    }
}
