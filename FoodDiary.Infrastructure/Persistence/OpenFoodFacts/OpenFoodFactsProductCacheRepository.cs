using FoodDiary.Application.Abstractions.OpenFoodFacts.Common;
using FoodDiary.Application.Abstractions.OpenFoodFacts.Models;
using FoodDiary.Domain.Entities.OpenFoodFacts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        foreach (OpenFoodFactsProductModel product in candidates) {
            var candidate = OpenFoodFactsProduct.Create(
                product.Barcode,
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
            await UpsertProductAsync(candidate, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpsertProductAsync(OpenFoodFactsProduct product, CancellationToken cancellationToken) {
        await context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "OpenFoodFactsProducts"
                ("Barcode", "Name", "Brand", "Category", "ImageUrl", "CaloriesPer100G", "ProteinsPer100G",
                 "FatsPer100G", "CarbsPer100G", "FiberPer100G", "LastSyncedAtUtc", "LastSeenAtUtc", "SearchHitCount")
            VALUES
                (@barcode, @name, @brand, @category, @imageUrl, @calories, @proteins,
                 @fats, @carbs, @fiber, @lastSynced, @lastSeen, @searchHitCount)
            ON CONFLICT ("Barcode") DO UPDATE SET
                "Name" = EXCLUDED."Name",
                "Brand" = EXCLUDED."Brand",
                "Category" = EXCLUDED."Category",
                "ImageUrl" = EXCLUDED."ImageUrl",
                "CaloriesPer100G" = EXCLUDED."CaloriesPer100G",
                "ProteinsPer100G" = EXCLUDED."ProteinsPer100G",
                "FatsPer100G" = EXCLUDED."FatsPer100G",
                "CarbsPer100G" = EXCLUDED."CarbsPer100G",
                "FiberPer100G" = EXCLUDED."FiberPer100G",
                "LastSyncedAtUtc" = EXCLUDED."LastSyncedAtUtc",
                "LastSeenAtUtc" = EXCLUDED."LastSeenAtUtc",
                "SearchHitCount" = "OpenFoodFactsProducts"."SearchHitCount" + 1
            """,
            [
                new NpgsqlParameter<string>("barcode", product.Barcode),
                new NpgsqlParameter<string>("name", product.Name),
                new NpgsqlParameter("brand", (object?)product.Brand ?? DBNull.Value),
                new NpgsqlParameter("category", (object?)product.Category ?? DBNull.Value),
                new NpgsqlParameter("imageUrl", (object?)product.ImageUrl ?? DBNull.Value),
                new NpgsqlParameter("calories", (object?)product.CaloriesPer100G ?? DBNull.Value),
                new NpgsqlParameter("proteins", (object?)product.ProteinsPer100G ?? DBNull.Value),
                new NpgsqlParameter("fats", (object?)product.FatsPer100G ?? DBNull.Value),
                new NpgsqlParameter("carbs", (object?)product.CarbsPer100G ?? DBNull.Value),
                new NpgsqlParameter("fiber", (object?)product.FiberPer100G ?? DBNull.Value),
                new NpgsqlParameter<DateTime>("lastSynced", product.LastSyncedAtUtc),
                new NpgsqlParameter<DateTime>("lastSeen", product.LastSeenAtUtc),
                new NpgsqlParameter<int>("searchHitCount", product.SearchHitCount),
            ],
            cancellationToken).ConfigureAwait(false);
    }

    private static string EscapeLikePattern(string value) {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
