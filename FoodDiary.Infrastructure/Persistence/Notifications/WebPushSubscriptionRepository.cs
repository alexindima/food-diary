using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Models;
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

        return await query.FirstOrDefaultAsync(x => x.Endpoint == endpoint, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebPushSubscription>> GetByUserAsync(UserId userId, CancellationToken cancellationToken = default) {
        return await context.WebPushSubscriptions
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.ModifiedOnUtc ?? x.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<WebPushSubscriptionReadModel>> GetByUserReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.WebPushSubscriptions
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
        await context.WebPushSubscriptions.AddAsync(subscription, cancellationToken).ConfigureAwait(false);
        return subscription;
    }

    public Task UpdateAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        context.WebPushSubscriptions.Update(subscription);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(WebPushSubscription subscription, CancellationToken cancellationToken = default) {
        context.WebPushSubscriptions.Remove(subscription);
        return Task.CompletedTask;
    }

    public Task DeleteRangeAsync(IReadOnlyCollection<WebPushSubscription> subscriptions, CancellationToken cancellationToken = default) {
        if (subscriptions.Count == 0) {
            return Task.CompletedTask;
        }

        context.WebPushSubscriptions.RemoveRange(subscriptions);
        return Task.CompletedTask;
    }
}
