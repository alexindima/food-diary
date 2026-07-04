using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.ShoppingLists;

public sealed class ShoppingListRepository(FoodDiaryDbContext context) : IShoppingListRepository {
    public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) {
        context.ShoppingLists.Add(list);
        return Task.FromResult(list);
    }

    public async Task<ShoppingList?> GetByIdAsync(
        ShoppingListId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<ShoppingList> query = context.ShoppingLists;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        if (includeItems) {
            query = query.AsSplitQuery()
                .Include(l => l.Items)
                .ThenInclude(i => i.Sources);
        }

        return await query.FirstOrDefaultAsync(
            list => list.Id == id && list.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ShoppingList?> GetCurrentAsync(
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<ShoppingList> query = context.ShoppingLists;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        if (includeItems) {
            query = query.AsSplitQuery()
                .Include(l => l.Items)
                .ThenInclude(i => i.Sources);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ShoppingList>> GetAllAsync(
        UserId userId,
        bool includeItems = false,
        CancellationToken cancellationToken = default) {
        IQueryable<ShoppingList> query = context.ShoppingLists.AsNoTracking();

        if (includeItems) {
            query = query.AsSplitQuery()
                .Include(l => l.Items)
                .ThenInclude(i => i.Sources);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) {
        context.ShoppingLists.Update(list);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) {
        ShoppingList? tracked = await context.ShoppingLists.FindAsync([list.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            context.ShoppingLists.Remove(tracked);
        }
    }
}
