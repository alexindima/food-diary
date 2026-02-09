using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class ShoppingListRepository : IShoppingListRepository
{
    private readonly FoodDiaryDbContext _context;

    public ShoppingListRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<ShoppingList> AddAsync(ShoppingList list)
    {
        _context.ShoppingLists.Add(list);
        await _context.SaveChangesAsync();
        return list;
    }

    public async Task<ShoppingList?> GetByIdAsync(
        ShoppingListId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<ShoppingList> query = _context.ShoppingLists;

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        if (includeItems)
        {
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
        CancellationToken cancellationToken = default)
    {
        IQueryable<ShoppingList> query = _context.ShoppingLists;

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        if (includeItems)
        {
            query = query.Include(l => l.Items);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpdateAsync(ShoppingList list)
    {
        _context.ShoppingLists.Update(list);
        await _context.SaveChangesAsync();
    }
}
