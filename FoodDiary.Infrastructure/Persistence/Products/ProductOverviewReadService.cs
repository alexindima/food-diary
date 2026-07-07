using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Products;

internal sealed class ProductOverviewReadService(FoodDiaryDbContext context) : IProductOverviewReadService {
    private const string LikeEscapeCharacter = "\\";

    public async Task<(IReadOnlyList<ProductOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = Math.Max(page, 1);
        int pageSize = Math.Max(limit, 1);

        IQueryable<Product> query = ApplyFilters(CreateBaseQuery(userId, includePublic), filters);

        int totalItems = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        List<ProductOverviewReadRow> rows = await ProjectRows(query
                .OrderByDescending(p => p.CreatedOnUtc)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (rows.ConvertAll(row => ToReadItem(row, userId)), totalItems);
    }

    public async Task<IReadOnlyDictionary<ProductId, ProductOverviewReadItem>> GetByIdsWithUsageAsync(
        IEnumerable<ProductId> ids,
        UserId userId,
        bool includePublic = true,
        CancellationToken cancellationToken = default) {
        var productIds = ids.Distinct().ToList();
        if (productIds.Count == 0) {
            return new Dictionary<ProductId, ProductOverviewReadItem>();
        }

        List<ProductOverviewReadRow> rows = await ProjectRows(CreateBaseQuery(userId, includePublic)
                .Where(p => productIds.Contains(p.Id)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return rows.ToDictionary(row => row.Id, row => ToReadItem(row, userId));
    }

    private IQueryable<Product> CreateBaseQuery(UserId userId, bool includePublic) =>
        context.Products
            .AsNoTracking()
            .Where(includePublic
                ? p => p.UserId == userId || p.Visibility == Visibility.Public
                : p => p.UserId == userId);

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductQueryFilters filters) {
        if (!string.IsNullOrWhiteSpace(filters.Search)) {
            string normalizedSearch = $"%{EscapeLikePattern(filters.Search.Trim())}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.Name, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Brand ?? string.Empty, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Category ?? string.Empty, normalizedSearch, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Barcode ?? string.Empty, normalizedSearch, LikeEscapeCharacter));
        }

        if (filters.ProductTypes is { Count: > 0 }) {
            query = query.Where(p => filters.ProductTypes.Contains(p.ProductType));
        }

        if (filters.CaloriesFrom.HasValue) {
            query = query.Where(p => p.CaloriesPerBase >= filters.CaloriesFrom.Value);
        }

        if (filters.CaloriesTo.HasValue) {
            query = query.Where(p => p.CaloriesPerBase <= filters.CaloriesTo.Value);
        }

        if (filters.HasImage.HasValue) {
            query = filters.HasImage.Value
                ? query.Where(p => p.ImageUrl != null || p.ImageAssetId != null)
                : query.Where(p => p.ImageUrl == null && p.ImageAssetId == null);
        }

        return query;
    }

    private static IQueryable<ProductOverviewReadRow> ProjectRows(IQueryable<Product> query) =>
        query.Select(product => new ProductOverviewReadRow(
            product.Id,
            product.UserId,
            product.Barcode,
            product.Name,
            product.Brand,
            product.ProductType,
            product.Category,
            product.Description,
            product.Comment,
            product.ImageUrl,
            product.ImageAssetId,
            product.BaseUnit,
            product.BaseAmount,
            product.DefaultPortionAmount,
            product.CaloriesPerBase,
            product.ProteinsPerBase,
            product.FatsPerBase,
            product.CarbsPerBase,
            product.FiberPerBase,
            product.AlcoholPerBase,
            product.MealItems.Count + product.RecipeIngredients.Count,
            product.Visibility,
            product.CreatedOnUtc,
            product.UsdaFdcId));

    private static ProductOverviewReadItem ToReadItem(ProductOverviewReadRow row, UserId currentUserId) {
        var quality = FoodQualityScore.Calculate(
            row.CaloriesPerBase,
            row.ProteinsPerBase,
            row.FatsPerBase,
            row.CarbsPerBase,
            row.FiberPerBase,
            row.AlcoholPerBase,
            row.ProductType);
        bool isOwnedByCurrentUser = row.UserId == currentUserId;

        return new ProductOverviewReadItem(
            row.Id,
            row.UserId,
            row.Barcode,
            row.Name,
            row.Brand,
            row.ProductType,
            row.Category,
            row.Description,
            isOwnedByCurrentUser ? row.Comment : null,
            row.ImageUrl,
            row.ImageAssetId,
            row.BaseUnit,
            row.BaseAmount,
            row.DefaultPortionAmount,
            row.CaloriesPerBase,
            row.ProteinsPerBase,
            row.FatsPerBase,
            row.CarbsPerBase,
            row.FiberPerBase,
            row.AlcoholPerBase,
            row.UsageCount,
            row.Visibility,
            row.CreatedOnUtc,
            isOwnedByCurrentUser,
            quality.Score,
            quality.Grade.ToString().ToLowerInvariant(),
            row.UsdaFdcId);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", @"\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }

    private sealed record ProductOverviewReadRow(
        ProductId Id,
        UserId UserId,
        string? Barcode,
        string Name,
        string? Brand,
        ProductType ProductType,
        string? Category,
        string? Description,
        string? Comment,
        string? ImageUrl,
        ImageAssetId? ImageAssetId,
        MeasurementUnit BaseUnit,
        double BaseAmount,
        double DefaultPortionAmount,
        double CaloriesPerBase,
        double ProteinsPerBase,
        double FatsPerBase,
        double CarbsPerBase,
        double FiberPerBase,
        double AlcoholPerBase,
        int UsageCount,
        Visibility Visibility,
        DateTime CreatedOnUtc,
        int? UsdaFdcId);
}
