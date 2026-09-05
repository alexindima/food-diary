using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

public sealed class WebPushSubscriptionRepository(DbSet<WebPushSubscription> subscriptions) : IWebPushSubscriptionRepository {
    public async Task<WebPushSubscription?> GetByEndpointAsync(
        string endpoint,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<WebPushSubscription> query = subscriptions;
        if (!asTracking) {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await subscriptions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ModifiedOnUtc ?? x.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await subscriptions
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ModifiedOnUtc ?? x.CreatedOnUtc)
            .Select(x => new WebPushSubscriptionReadModel(
                x.Endpoint,
                x.ExpirationTimeUtc,
                x.Locale,
                x.UserAgent,
                x.CreatedOnUtc,
                x.ModifiedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<WebPushSubscription> AddAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        await subscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        return subscription;
    }

    public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        subscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        subscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> items, CancellationToken cancellationToken = default) {
        if (items.Count == 0) {
            return Task.CompletedTask;
        }

        subscriptions.RemoveRange(items);
        return Task.CompletedTask;
    }
}
