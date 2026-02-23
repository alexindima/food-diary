using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class ShoppingListRepository(FoodDiaryDbContext context) : IShoppingListRepository {
    public async Task<ShoppingList> AddAsync(ShoppingList list) {
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return list;
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
            query = query.Include(l => l.Items);
        }

        return await query.FirstOrDefaultAsync(
            list => list.Id == id && list.UserId == userId,
            cancellationToken);
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
            query = query.Include(l => l.Items);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ShoppingList>> GetAllAsync(
        UserId userId,
        bool includeItems = false,
        CancellationToken cancellationToken = default) {
        var query = context.ShoppingLists.AsNoTracking();

        if (includeItems) {
            query = query.Include(l => l.Items);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(ShoppingList list) {
        context.ShoppingLists.Update(list);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(ShoppingList list) {
        context.ShoppingLists.Remove(list);
        await context.SaveChangesAsync();
    }
}
