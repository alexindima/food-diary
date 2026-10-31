using FoodDiary.Modules.Notifications.Application.Abstractions.Common;
using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Notifications.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Notifications.Infrastructure.Persistence;

internal sealed class NotificationReadStatusRepository(DbSet<Notification> notifications) : INotificationReadStatusRepository {
    public async Task<IReadOnlyList<Guid>> GetReadIdsAsync(
        UserId userId,
        IReadOnlyCollection<NotificationId> notificationIds,
        CancellationToken cancellationToken) =>
        await notifications.AsNoTracking()
            .Where(notification => notification.UserId == userId && notification.IsRead && notificationIds.Contains(notification.Id))
            .Select(notification => notification.Id.Value)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
}
