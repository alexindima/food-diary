using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Favorites.Infrastructure.Persistence.FavoriteMeals;

public sealed class FavoriteMealQuery(ICompositionReadContext context) : IFavoriteMealQuery {
    public async Task<IReadOnlyList<FavoriteMealReadModel>> GetAllReadModelsAsync(
        UserId userId, CancellationToken cancellationToken = default) =>
        await ReadItemsAsync(userId, PaginationPolicy.MaxCollectionSize, cancellationToken).ConfigureAwait(false);

    public async Task<(IReadOnlyList<FavoriteMealReadModel> Items, int TotalItems)> GetOverviewReadModelsAsync(
        UserId userId, int limit, CancellationToken cancellationToken = default) {
        int totalItems = await context.FavoriteMeals.AsNoTracking()
            .Where(f => f.UserId == userId)
            .Join(context.Meals.AsNoTracking(), favorite => favorite.MealId, source => source.Id,
                (favorite, source) => favorite.Id)
            .CountAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<FavoriteMealReadModel> items = await ReadItemsAsync(
            userId, Math.Clamp(limit, 0, PaginationPolicy.MaxCollectionSize), cancellationToken).ConfigureAwait(false);
        return (items, totalItems);
    }

    public async Task<(IReadOnlyList<FavoriteMealReadModel> Items, int TotalItems)> GetPageReadModelsAsync(
        UserId userId, int page, int limit, string? search, CancellationToken cancellationToken = default) {
        var rows = context.FavoriteMeals.AsNoTracking().Where(f => f.UserId == userId)
            .Join(context.Meals.AsNoTracking().Where(meal => meal.UserId == userId),
                favorite => favorite.MealId, meal => meal.Id, (favorite, meal) => new { Favorite = favorite, Meal = meal });
        if (!string.IsNullOrWhiteSpace(search)) {
            string term = "%" + search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            rows = rows.Where(row => (row.Favorite.Name != null && EF.Functions.ILike(row.Favorite.Name, term, "\\"))
                || row.Meal.Items.Any(item => EF.Functions.ILike((item.SnapshotName
                    ?? context.Products.AsNoTracking().Where(product => product.Id == item.ProductId).Select(product => product.Name).FirstOrDefault()
                    ?? context.Recipes.AsNoTracking().Where(recipe => recipe.Id == item.RecipeId).Select(recipe => recipe.Name).FirstOrDefault()
                    ?? string.Empty), term, "\\"))
                || row.Meal.AiSessions.Any(session => session.Items.Any(item => EF.Functions.ILike(item.NameLocal ?? item.NameEn, term, "\\"))));
        }
        int total = await rows.CountAsync(cancellationToken).ConfigureAwait(false);
        List<FavoriteMealReadModel> items = await rows.OrderByDescending(row => row.Favorite.CreatedAtUtc).ThenByDescending(row => row.Favorite.Id)
            .Skip((page - 1) * limit).Take(limit)
            .Select(row => new FavoriteMealReadModel(row.Favorite.Id.Value, row.Favorite.MealId.Value, row.Favorite.Name,
                row.Favorite.CreatedAtUtc, row.Meal.Date, row.Meal.MealType == null ? null : row.Meal.MealType.ToString(),
                row.Meal.TotalCalories, row.Meal.TotalProteins, row.Meal.TotalFats, row.Meal.TotalCarbs, row.Meal.Items.Count) {
                TotalFiber = row.Meal.TotalFiber,
                ItemImageUrls = row.Meal.Items.OrderBy(item => item.CreatedOnUtc).ThenBy(item => item.Id)
                    .Select(item => item.SnapshotImageUrl
                        ?? context.Products.AsNoTracking().Where(product => product.Id == item.ProductId).Select(product => product.ImageUrl).FirstOrDefault()
                        ?? context.Recipes.AsNoTracking().Where(recipe => recipe.Id == item.RecipeId).Select(recipe => recipe.ImageUrl).FirstOrDefault())
                    .Where(url => url != null && url != string.Empty).Take(4).Select(url => url!).ToList(),
                ImageUrl = row.Meal.ImageUrl
                    ?? context.ImageAssets.AsNoTracking().Where(asset => asset.Id == row.Meal.ImageAssetId).Select(asset => asset.Url).FirstOrDefault(),
                AiImageUrls = row.Meal.AiSessions.Where(session => session.ImageAssetId != null)
                    .OrderBy(session => session.CreatedOnUtc).ThenBy(session => session.Id)
                    .Select(session => context.ImageAssets.AsNoTracking().Where(asset => asset.Id == session.ImageAssetId).Select(asset => asset.Url).FirstOrDefault())
                    .Where(url => url != null && url != string.Empty).Take(4).Select(url => url!).ToList(),
                AiItemNames = row.Meal.AiSessions.SelectMany(session => session.Items).OrderBy(item => item.CreatedOnUtc).ThenBy(item => item.Id)
                    .Select(item => item.NameLocal ?? item.NameEn).Take(4).ToList(),
                ItemNames = row.Meal.Items.OrderBy(item => item.CreatedOnUtc).ThenBy(item => item.Id)
                    .Select(item => (item.SnapshotName
                    ?? context.Products.AsNoTracking().Where(product => product.Id == item.ProductId).Select(product => product.Name).FirstOrDefault()
                    ?? context.Recipes.AsNoTracking().Where(recipe => recipe.Id == item.RecipeId).Select(recipe => recipe.Name).FirstOrDefault()
                    ?? string.Empty)).Where(name => name != string.Empty).Take(4).ToList(),
            }).ToListAsync(cancellationToken).ConfigureAwait(false);
        return (items, total);
    }

    private async Task<IReadOnlyList<FavoriteMealReadModel>> ReadItemsAsync(
        UserId userId, int limit, CancellationToken cancellationToken) {
        if (limit == 0) {
            return [];
        }
        return await context.FavoriteMeals
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ThenByDescending(f => f.Id)
            .Take(limit)
            .Join(context.Meals.AsNoTracking(), favorite => favorite.MealId, source => source.Id, (favorite, source) => new { Favorite = favorite, Source = source })
            .Select(row => new FavoriteMealReadModel(
                row.Favorite.Id.Value,
                row.Favorite.MealId.Value,
                row.Favorite.Name,
                row.Favorite.CreatedAtUtc,
                row.Source.Date,
                row.Source.MealType == null ? null : row.Source.MealType.ToString(),
                row.Source.TotalCalories,
                row.Source.TotalProteins,
                row.Source.TotalFats,
                row.Source.TotalCarbs,
                row.Source.Items.Count) { TotalFiber = row.Source.TotalFiber, ImageUrl = row.Source.ImageUrl })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }
}
