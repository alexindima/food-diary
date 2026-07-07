using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.RecentItems.Common;

internal static class RecentItemOverviewLoader {
    public static async Task<IReadOnlyList<TItem>> LoadAsync<TRecent, TId, TItem>(
        UserId userId,
        int limit,
        Func<UserId, int, CancellationToken, Task<IReadOnlyList<TRecent>>> getRecentsAsync,
        Func<TRecent, TId> selectId,
        Func<IReadOnlyList<TId>, UserId, CancellationToken, Task<IReadOnlyDictionary<TId, TItem>>> getItemsByIdAsync,
        CancellationToken cancellationToken)
        where TId : notnull {
        IReadOnlyList<TRecent> recents = await getRecentsAsync(userId, limit, cancellationToken).ConfigureAwait(false);
        if (recents.Count == 0) {
            return [];
        }

        TId[] idsInOrder = [.. recents.Select(selectId)];
        IReadOnlyDictionary<TId, TItem> itemsById = await getItemsByIdAsync(idsInOrder, userId, cancellationToken).ConfigureAwait(false);

        return [.. idsInOrder
            .Where(itemsById.ContainsKey)
            .Select(id => itemsById[id])];
    }
}
