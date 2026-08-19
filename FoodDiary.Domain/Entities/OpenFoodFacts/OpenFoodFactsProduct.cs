using FoodDiary.Domain.Common;

namespace FoodDiary.Domain.Entities.OpenFoodFacts;

public sealed class OpenFoodFactsProduct {
    private const int BarcodeMaxLength = 64;
    private const int NameMaxLength = 512;
    private const int BrandMaxLength = 512;
    private const int CategoryMaxLength = 1024;
    private const int ImageUrlMaxLength = 2048;

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
            Barcode = DomainGuard.RequiredText(barcode, BarcodeMaxLength, nameof(barcode)),
            SearchHitCount = 0,
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
        string normalizedName = DomainGuard.RequiredText(name, NameMaxLength, nameof(name));
        string? normalizedBrand = DomainGuard.OptionalText(brand, BrandMaxLength, nameof(brand));
        string? normalizedCategory = DomainGuard.OptionalText(category, CategoryMaxLength, nameof(category));
        string? normalizedImageUrl = DomainGuard.OptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));
        double? normalizedCalories = DomainGuard.NonNegativeFinite(caloriesPer100G, nameof(caloriesPer100G));
        double? normalizedProteins = DomainGuard.NonNegativeFinite(proteinsPer100G, nameof(proteinsPer100G));
        double? normalizedFats = DomainGuard.NonNegativeFinite(fatsPer100G, nameof(fatsPer100G));
        double? normalizedCarbs = DomainGuard.NonNegativeFinite(carbsPer100G, nameof(carbsPer100G));
        double? normalizedFiber = DomainGuard.NonNegativeFinite(fiberPer100G, nameof(fiberPer100G));
        DateTime normalizedSyncedAtUtc = EnsureUtc(syncedAtUtc);

        Name = normalizedName;
        Brand = normalizedBrand;
        Category = normalizedCategory;
        ImageUrl = normalizedImageUrl;
        CaloriesPer100G = normalizedCalories;
        ProteinsPer100G = normalizedProteins;
        FatsPer100G = normalizedFats;
        CarbsPer100G = normalizedCarbs;
        FiberPer100G = normalizedFiber;
        LastSyncedAtUtc = normalizedSyncedAtUtc;
        MarkSeen(normalizedSyncedAtUtc);
    }

    public void MarkSeen(DateTime seenAtUtc) {
        LastSeenAtUtc = EnsureUtc(seenAtUtc);
        if (SearchHitCount < int.MaxValue) {
            SearchHitCount++;
        }
    }

    private static DateTime EnsureUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
