using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

public sealed class WebPushSubscriptionRepository(FoodDiaryDbContext context) : IWebPushSubscriptionRepository {
    public async Task<WebPushSubscription?> GetByEndpointAsync(
        string endpoint,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<WebPushSubscription> query = context.WebPushSubscriptions;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken);
    }

    public async Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.WebPushSubscriptions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ModifiedOnUtc ?? x.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        context.WebPushSubscriptions.Add(subscription);
        await context.SaveChangesAsync(cancellationToken);
        return subscription;
    }

    public async Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        context.WebPushSubscriptions.Update(subscription);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        context.WebPushSubscriptions.Remove(subscription);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptions, CancellationToken cancellationToken = default) {
        if (subscriptions.Count == 0) {
            return;
        }

        context.WebPushSubscriptions.RemoveRange(subscriptions);
        await context.SaveChangesAsync(cancellationToken);
    }
}
