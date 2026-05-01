namespace FoodDiary.Domain.Entities.OpenFoodFacts;

public class OpenFoodFactsProduct {
    public string Barcode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Category { get; private set; }
    public string? ImageUrl { get; private set; }
    public double? CaloriesPer100G { get; private set; }
    public double? ProteinsPer100G { get; private set; }
    public double? FatsPer100G { get; private set; }
    public double? CarbsPer100G { get; private set; }
    public double? FiberPer100G { get; private set; }
    public DateTime LastSyncedAtUtc { get; private set; }
    public DateTime LastSeenAtUtc { get; private set; }
    public int SearchHitCount { get; private set; }

    private OpenFoodFactsProduct() {
    }

    public static OpenFoodFactsProduct Create(
        string barcode,
        string name,
        string? brand,
        string? category,
        string? imageUrl,
        double? caloriesPer100G,
        double? proteinsPer100G,
        double? fatsPer100G,
        double? carbsPer100G,
        double? fiberPer100G,
        DateTime syncedAtUtc) {
        var product = new OpenFoodFactsProduct {
            Barcode = NormalizeRequired(barcode),
            SearchHitCount = 0
        };

        product.Update(
            name,
            brand,
            category,
            imageUrl,
            caloriesPer100G,
            proteinsPer100G,
            fatsPer100G,
            carbsPer100G,
            fiberPer100G,
            syncedAtUtc);

        return product;
    }

    public void Update(
        string name,
        string? brand,
        string? category,
        string? imageUrl,
        double? caloriesPer100G,
        double? proteinsPer100G,
        double? fatsPer100G,
        double? carbsPer100G,
        double? fiberPer100G,
        DateTime syncedAtUtc) {
        Name = NormalizeRequired(name);
        Brand = NormalizeOptional(brand);
        Category = NormalizeOptional(category);
        ImageUrl = NormalizeOptional(imageUrl);
        CaloriesPer100G = caloriesPer100G;
        ProteinsPer100G = proteinsPer100G;
        FatsPer100G = fatsPer100G;
        CarbsPer100G = carbsPer100G;
        FiberPer100G = fiberPer100G;
        LastSyncedAtUtc = EnsureUtc(syncedAtUtc);
        MarkSeen(syncedAtUtc);
    }

    public void MarkSeen(DateTime seenAtUtc) {
        LastSeenAtUtc = EnsureUtc(seenAtUtc);
        SearchHitCount++;
    }

    private static string NormalizeRequired(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Value is required.", nameof(value));
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
