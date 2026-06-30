using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Domain.Entities.OpenFoodFacts;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.OpenFoodFacts;

internal sealed class OpenFoodFactsProductCacheRepository(FoodDiaryDbContext context, TimeProvider timeProvider) : IOpenFoodFactsProductCacheRepository {
    private const string LikeEscapeCharacter = "\\";

    public async Task<IReadOnlyList<OpenFoodFactsProductModel>> SearchAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default) {
        string normalizedQuery = query.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery)) {
            return [];
        }

        string pattern = $"%{EscapeLikePattern(normalizedQuery)}%";
        return await context.OpenFoodFactsProducts
            .AsNoTracking()
            .Where(product =>
                EF.Functions.ILike(product.Name, pattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(product.Brand ?? string.Empty, pattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(product.Category ?? string.Empty, pattern, LikeEscapeCharacter) ||
                EF.Functions.ILike(product.Barcode, pattern, LikeEscapeCharacter))
            .OrderByDescending(product => EF.Functions.ILike(product.Name, $"{EscapeLikePattern(normalizedQuery)}%", LikeEscapeCharacter))
            .ThenByDescending(product => product.SearchHitCount)
            .ThenByDescending(product => product.LastSeenAtUtc)
            .ThenBy(product => product.Name.Length)
            .ThenBy(product => product.Name)
            .Take(Math.Max(limit, 1))
            .Select(product => new OpenFoodFactsProductModel(
                product.Barcode,
                product.Name,
                product.Brand,
                product.Category,
                product.ImageUrl,
                product.CaloriesPer100G,
                product.ProteinsPer100G,
                product.FatsPer100G,
                product.CarbsPer100G,
                product.FiberPer100G))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpsertAsync(
        IReadOnlyCollection<OpenFoodFactsProductModel> products,
        CancellationToken cancellationToken = default) {
        var candidates = products
            .Where(product => !string.IsNullOrWhiteSpace(product.Barcode) && !string.IsNullOrWhiteSpace(product.Name))
            .DistinctBy(product => product.Barcode.Trim())
            .ToList();
        if (candidates.Count == 0) {
            return;
        }

        List<string> barcodes = candidates.ConvertAll(product => product.Barcode.Trim());
        Dictionary<string, OpenFoodFactsProduct> existingProducts = await context.OpenFoodFactsProducts
            .Where(product => barcodes.Contains(product.Barcode))
            .ToDictionaryAsync(product => product.Barcode, cancellationToken).ConfigureAwait(false);
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (OpenFoodFactsProductModel product in candidates) {
            string barcode = product.Barcode.Trim();
            if (existingProducts.TryGetValue(barcode, out OpenFoodFactsProduct? existingProduct)) {
                existingProduct.Update(
                    product.Name,
                    product.Brand,
                    product.Category,
                    product.ImageUrl,
                    product.CaloriesPer100G,
                    product.ProteinsPer100G,
                    product.FatsPer100G,
                    product.CarbsPer100G,
                    product.FiberPer100G,
                    now);
                continue;
            }

            context.OpenFoodFactsProducts.Add(OpenFoodFactsProduct.Create(
                barcode,
                product.Name,
                product.Brand,
                product.Category,
                product.ImageUrl,
                product.CaloriesPer100G,
                product.ProteinsPer100G,
                product.FatsPer100G,
                product.CarbsPer100G,
                product.FiberPer100G,
                now));
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
