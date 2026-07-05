using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Models;
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

    public async Task<ShoppingListReadModel?> GetReadModelByIdAsync(
        ShoppingListId id,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModel(context.ShoppingLists
                .AsNoTracking()
                .Where(list => list.Id == id && list.UserId == userId))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
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

    public async Task<ShoppingListReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModel(context.ShoppingLists
                .AsNoTracking()
                .Where(list => list.UserId == userId)
                .OrderByDescending(list => list.CreatedOnUtc))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
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

    public async Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.ShoppingLists
            .AsNoTracking()
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .Select(list => new ShoppingListSummaryReadModel(
                list.Id.Value,
                list.Name,
                list.CreatedOnUtc,
                list.Items.Count))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
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

    private static IQueryable<ShoppingListReadModel> ProjectReadModel(IQueryable<ShoppingList> query) {
        return query
            .Select(list => new ShoppingListReadModel(
                list.Id.Value,
                list.Name,
                list.CreatedOnUtc,
                list.Items
                    .OrderBy(item => item.SortOrder)
                    .ThenBy(item => item.Name)
                    .Select(item => new ShoppingListItemReadModel(
                        item.Id.Value,
                        item.ShoppingListId.Value,
                        item.ProductId.HasValue ? item.ProductId.Value.Value : null,
                        item.Name,
                        item.Amount,
                        item.Unit == null ? null : item.Unit.ToString(),
                        item.Category,
                        item.Aisle,
                        item.Note,
                        item.IsChecked,
                        item.CheckedOnUtc,
                        item.SortOrder,
                        item.Sources
                            .OrderBy(source => source.DayNumber ?? int.MaxValue)
                            .ThenBy(source => source.Label)
                            .Select(source => new ShoppingListItemSourceReadModel(
                                source.Id.Value,
                                source.SourceType.ToString(),
                                source.MealPlanId.HasValue ? source.MealPlanId.Value.Value : null,
                                source.MealPlanMealId.HasValue ? source.MealPlanMealId.Value.Value : null,
                                source.RecipeId.HasValue ? source.RecipeId.Value.Value : null,
                                source.Label,
                                source.DayNumber,
                                source.MealType,
                                source.Amount,
                                source.Unit == null ? null : source.Unit.ToString()))
                            .ToList()))
                    .ToList()))
            .AsSplitQuery();
    }
}
