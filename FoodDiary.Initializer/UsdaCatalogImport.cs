using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Modules.Products.Application.Commands.ImportUsdaCatalog;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;

namespace FoodDiary.Initializer;

internal sealed class UsdaCatalogImport(
    IUsdaCatalogReadService source,
    UsdaCatalogProductImporter importer,
    SystemContentOwnerProvisioner owner) {
    private static readonly JsonSerializerOptions ManifestJson = new(JsonSerializerDefaults.Web) {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public async Task RunAsync(string manifestPath, bool dryRun, CancellationToken cancellationToken) {
        await using FileStream stream = File.OpenRead(manifestPath);
        Manifest? manifest = await JsonSerializer.DeserializeAsync<Manifest>(stream, ManifestJson, cancellationToken);
        if (manifest is not { Version: 1, Products.Count: > 0 } ||
            manifest.Products.Exists(item => item is null || item.FdcId <= 0 || string.IsNullOrWhiteSpace(item.Name) ||
                string.IsNullOrWhiteSpace(item.ProductType) || string.IsNullOrWhiteSpace(item.Category)) ||
            manifest.Products.Select(item => item.FdcId).Distinct().Count() != manifest.Products.Count) {
            throw new InvalidOperationException("Use a version 1 manifest with unique positive FDC IDs and complete names and categories.");
        }

        IReadOnlyDictionary<int, UsdaCatalogFoodModel> foods =
            await source.ReadLocalFoodsAsync(manifest.Products.Select(item => item.FdcId).ToArray(), cancellationToken);
        var candidates = new List<UsdaCatalogProduct>(manifest.Products.Count);
        foreach (ManifestProduct item in manifest.Products) {
            if (!foods.TryGetValue(item.FdcId, out UsdaCatalogFoodModel? food)) {
                throw new InvalidOperationException(FormattableString.Invariant($"USDA {item.FdcId} is missing. Run seed-usda with the SR Legacy archive first."));
            }
            IReadOnlyList<UsdaNutrientReadModel> values = food.Nutrients;
            candidates.Add(new UsdaCatalogProduct(item.FdcId, item.Name, food.Description, item.ProductType, item.Category,
                ReadAmount(values, 1008, "kcal", item.FdcId),
                ReadAmount(values, 1003, "g", item.FdcId),
                ReadAmount(values, 1004, "g", item.FdcId),
                ReadAmount(values, 1005, "g", item.FdcId),
                ReadAmount(values, 1079, "g", item.FdcId),
                ReadAmount(values, 1018, "g", item.FdcId)));
        }

        IReadOnlySet<int> changed = await importer.FindNewAsync(candidates, cancellationToken);
        if (!dryRun && changed.Count > 0) {
            await owner.EnsureAsync(cancellationToken);
            changed = await importer.ImportAsync(candidates, cancellationToken);
        }

        string status = dryRun ? "ready" : "imported";
        foreach (UsdaCatalogProduct item in candidates) {
            string outcome = changed.Contains(item.FdcId) ? status : "skipped";
            Console.WriteLine(FormattableString.Invariant($"{outcome}: USDA {item.FdcId} {item.Name} ({item.CaloriesPer100G} kcal/100 g)"));
        }
        Console.WriteLine(FormattableString.Invariant($"{status}: {changed.Count}; skipped: {candidates.Count - changed.Count}"));
    }

    private static double ReadAmount(
        IReadOnlyList<UsdaNutrientReadModel> values, int nutrientId, string unit, int fdcId) {
        UsdaNutrientReadModel? nutrient = values.FirstOrDefault(value => value.NutrientId == nutrientId);
        if (nutrient is null || !string.Equals(nutrient.Unit, unit, StringComparison.OrdinalIgnoreCase) ||
            !double.IsFinite(nutrient.Amount) || nutrient.Amount < 0) {
            throw new InvalidOperationException(FormattableString.Invariant($"Invalid or missing USDA nutrient {nutrientId} for food {fdcId}. Expected {unit} per 100 g."));
        }
        return nutrient.Amount;
    }

    private sealed record Manifest(int Version, List<ManifestProduct> Products);
    private sealed record ManifestProduct(int FdcId, string Name, string ProductType, string Category);
}
