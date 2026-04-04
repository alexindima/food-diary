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

    public async Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
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
}
