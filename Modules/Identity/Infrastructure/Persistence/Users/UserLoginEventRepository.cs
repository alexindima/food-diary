using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Users;

public sealed class UserLoginEventRepository(DbSet<UserLoginEvent> events, IUserLoginEventQuery query, Func<CancellationToken, Task>? synchronizeTransactionAsync = null) : IUserLoginEventRepository {
    public async Task AddAsync(UserLoginEvent loginEvent, CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        await events.AddAsync(loginEvent, cancellationToken).ConfigureAwait(false);
    }

    public Task<(IReadOnlyList<UserLoginEventReadModel> Items, int TotalItems)> GetPagedAsync(
        int page, int limit, Guid? userId, string? search,
        CancellationToken cancellationToken = default, DateTimeOffset? fromUtc = null, DateTimeOffset? toUtc = null, string? provider = null, string? device = null) =>
        query.GetPagedAsync(page, limit, userId, search, cancellationToken, fromUtc, toUtc, provider, device);

    public Task<IReadOnlyList<UserLoginDeviceSummaryModel>> GetDeviceSummaryAsync(
        DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken = default) =>
        query.GetDeviceSummaryAsync(fromUtc, toUtc, cancellationToken);

    public async Task<int> DeleteOlderThanAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken cancellationToken = default) {
        if (synchronizeTransactionAsync is not null) {
            await synchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        }
        Guid[] ids = await events
            .AsNoTracking()
            .Where(item => item.LoggedInAtUtc < olderThanUtc)
            .OrderBy(item => item.LoggedInAtUtc)
            .Select(item => item.Id)
            .Take(Math.Max(batchSize, 1))
            .ToArrayAsync(cancellationToken).ConfigureAwait(false);

        if (ids.Length == 0) {
            return 0;
        }

        return await events
            .Where(item => Enumerable.Contains(ids, item.Id))
            .ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
    }

}
