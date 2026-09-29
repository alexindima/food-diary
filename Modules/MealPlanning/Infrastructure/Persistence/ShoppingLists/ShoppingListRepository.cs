using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.ShoppingLists.Models;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.MealPlanning.Infrastructure.Persistence.ShoppingLists;

public sealed class ShoppingListRepository(DbSet<ShoppingList> entries) : IShoppingListRepository {
    public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) {
        entries.Add(list);
        return Task.FromResult(list);
    }

    public async Task<ShoppingList?> GetByIdAsync(
        ShoppingListId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default) {
        IQueryable<ShoppingList> query = entries;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        if (includeItems) {
            query = IncludeItemsAndSources(query);
        }

        return await query.FirstOrDefaultAsync(
            list => list.Id == id && list.UserId == userId,
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ShoppingListReadModel?> GetReadModelByIdAsync(
        ShoppingListId id,
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModel(entries
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
        IQueryable<ShoppingList> query = entries;

        if (!asTracking) {
            query = query.AsNoTracking();
        }

        if (includeItems) {
            query = IncludeItemsAndSources(query);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<ShoppingListReadModel?> GetCurrentReadModelAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModel(entries
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
        IQueryable<ShoppingList> query = entries.AsNoTracking();

        if (includeItems) {
            query = IncludeItemsAndSources(query);
        }

        return await query
            .Where(list => list.UserId == userId)
            .OrderByDescending(list => list.CreatedOnUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ShoppingListSummaryReadModel>> GetAllSummaryReadModelsAsync(
        UserId userId,
        CancellationToken cancellationToken = default, int page = 1, int? pageSize = null, string? search = null) {
        IQueryable<ShoppingList> query = entries.AsNoTracking().Where(list => list.UserId == userId);
        if (!string.IsNullOrWhiteSpace(search)) {
            string term = search.Trim().ToLowerInvariant();
            // The parameterless overload is translated to SQL LOWER; culture overloads are not translatable.
#pragma warning disable MA0011
            query = query.Where(list => list.Name.ToLower().Contains(term));
#pragma warning restore MA0011
        }
        int size = pageSize.HasValue ? Math.Clamp(pageSize.Value, 1, 50) : PaginationPolicy.MaxCollectionSize;
        int offset = (Math.Clamp(page, 1, 10000) - 1) * size;
        return await query
            .OrderByDescending(list => list.CreatedOnUtc)
            .ThenByDescending(list => list.Id)
            .Skip(offset)
            .Take(size)
            .Select(list => new ShoppingListSummaryReadModel(
                list.Id.Value, list.Name, list.CreatedOnUtc, list.Items.Count,
                list.Items.Count(item => !item.IsChecked)))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) {
        entries.Update(list);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) {
        ShoppingList? tracked = await entries.FindAsync([list.Id], cancellationToken).ConfigureAwait(false);
        if (tracked is not null) {
            entries.Remove(tracked);
        }
    }

    private static IQueryable<ShoppingList> IncludeItemsAndSources(IQueryable<ShoppingList> query) =>
        query
            .AsSplitQuery()
            .Include(list => list.Items)
            .ThenInclude(item => item.Sources);

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
