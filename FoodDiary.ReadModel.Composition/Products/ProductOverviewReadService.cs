using FoodDiary.Modules.Products.FoodQuality.ValueObjects;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Contracts.Models;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.Products;

public sealed class ProductOverviewReadService(ICompositionReadContext context) : IProductOverviewReadService {
    private const string LikeEscapeCharacter = "\\";

    public async Task<(IReadOnlyList<ProductOverviewReadItem> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        ProductQueryFilters filters,
        CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);

        string? search = string.IsNullOrWhiteSpace(filters.Search) ? null : EscapeLikePattern(filters.Search.Trim());
        IQueryable<Product> query = ApplyFilters(CreateBaseQuery(userId, includePublic), filters, search);

        int totalItems = await query.AsNoTracking().CountAsync(cancellationToken);
        List<ProductOverviewReadRow> rows = await ProjectRows(OrderProducts(query, search, filters.Language)
                .AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize))
            .ToListAsync(cancellationToken);

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

        List<ProductOverviewReadRow> rows = await ProjectRows(CreateBaseQuery(userId, includePublic).AsNoTracking()
                .Where(p => productIds.Contains(p.Id)))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.Id, row => ToReadItem(row, userId));
    }

    private IQueryable<Product> CreateBaseQuery(UserId userId, bool includePublic) =>
        context.Products
            .AsNoTracking()
            .Where(includePublic
                ? p => p.UserId == userId || p.Visibility == Visibility.Public
                : p => p.UserId == userId);

    private static IQueryable<Product> ApplyFilters(IQueryable<Product> query, ProductQueryFilters filters, string? search) {
        if (search is not null) {
            string pattern = $"%{search}%";
            query = query.AsNoTracking().Where(p =>
                EF.Functions.ILike(p.Name, pattern, LikeEscapeCharacter) ||
                (p.UsdaEnglishName != null && EF.Functions.ILike(p.UsdaEnglishName, pattern, LikeEscapeCharacter)) ||
                EF.Functions.ILike(p.Brand ?? string.Empty, pattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Category ?? string.Empty, pattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(p.Barcode ?? string.Empty, pattern, LikeEscapeCharacter));
        }

        if (filters.ProductTypes is { Count: > 0 }) {
            query = query.AsNoTracking().Where(p => filters.ProductTypes.Contains(p.ProductType));
        }

        if (filters.CaloriesFrom.HasValue) {
            query = query.AsNoTracking().Where(p => p.CaloriesPerBase >= filters.CaloriesFrom.Value);
        }

        if (filters.CaloriesTo.HasValue) {
            query = query.AsNoTracking().Where(p => p.CaloriesPerBase <= filters.CaloriesTo.Value);
        }

        if (filters.HasImage.HasValue) {
            query = filters.HasImage.Value
                ? query.AsNoTracking().Where(p => p.ImageUrl != null || p.ImageAssetId != null)
                : query.AsNoTracking().Where(p => p.ImageUrl == null && p.ImageAssetId == null);
        }

        return query;
    }

    private static IQueryable<Product> OrderProducts(IQueryable<Product> query, string? search, ProductCatalogLanguage language) {
        if (search is null) {
            return query.AsNoTracking().OrderByDescending(p => p.CreatedOnUtc).ThenBy(p => p.Id);
        }

        string prefix = $"{search}%";
        string anywhere = $"%{search}%";
        return query.AsNoTracking().Select(p => new {
            Product = p,
            ExactBarcode = p.Barcode != null && EF.Functions.ILike(p.Barcode, search, LikeEscapeCharacter),
            NameRank = (EF.Functions.ILike(p.Name, search, LikeEscapeCharacter) ? 1 : 0) +
                    (EF.Functions.ILike(p.Name, prefix, LikeEscapeCharacter) ? 1 : 0) +
                    (EF.Functions.ILike(p.Name, anywhere, LikeEscapeCharacter) ? 1 : 0),
            EnglishRank = (EF.Functions.ILike(p.UsdaEnglishName ?? string.Empty, search, LikeEscapeCharacter) ? 1 : 0) +
                    (EF.Functions.ILike(p.UsdaEnglishName ?? string.Empty, prefix, LikeEscapeCharacter) ? 1 : 0) +
                    (EF.Functions.ILike(p.UsdaEnglishName ?? string.Empty, anywhere, LikeEscapeCharacter) ? 1 : 0),
        })
            .OrderByDescending(match => match.ExactBarcode)
            .ThenByDescending(match => Math.Max(match.NameRank, match.EnglishRank))
            .ThenByDescending(match => match.Product.UsdaEnglishName != null && (
                (language == ProductCatalogLanguage.English && match.EnglishRank > 0 && match.EnglishRank >= match.NameRank) ||
                (language == ProductCatalogLanguage.Russian && match.NameRank > 0 && match.NameRank >= match.EnglishRank)))
            .ThenByDescending(match => match.Product.CreatedOnUtc)
            .ThenBy(match => match.Product.Id)
            .Select(match => match.Product);
    }

    private IQueryable<ProductOverviewReadRow> ProjectRows(IQueryable<Product> query) =>
        query.AsNoTracking().Select(product => new ProductOverviewReadRow(
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
            context.MealItems.AsNoTracking().Count(item => item.ProductId == product.Id) + context.RecipeIngredients.AsNoTracking().Count(ingredient => ingredient.ProductId == product.Id),
            product.Visibility,
            product.CreatedOnUtc,
            product.UsdaFdcId,
            product.UsdaEnglishName,
            product.Images.OrderBy(image => image.Position).Select(image => new ProductImageReadItem(image.ImageAssetId.Value, image.ImageUrl)).ToList()));

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
            row.UsdaFdcId) { Images = row.Images, UsdaEnglishName = row.UsdaEnglishName };
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
        int? UsdaFdcId, string? UsdaEnglishName, IReadOnlyList<ProductImageReadItem> Images);
}
