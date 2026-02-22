using System;
using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Базовый продукт питания - корень агрегата
/// Представляет простой продукт (например, с штрихкодом из магазина)
/// </summary>
public sealed class Product : AggregateRoot<ProductId> {
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Category { get; private set; }
    public string? Description { get; private set; }
    public string? Comment { get; private set; }
    public string? ImageUrl { get; private set; }
    public ImageAssetId? ImageAssetId { get; private set; }
    public ProductType ProductType { get; private set; } = ProductType.Unknown;
    public MeasurementUnit BaseUnit { get; private set; }
    public double BaseAmount { get; private set; }
    /// <summary>
    /// Размер продукта (например, масса упаковки), используется как значение по умолчанию при добавлении в рецепты
    /// </summary>
    public double DefaultPortionAmount { get; private set; }
    public double CaloriesPerBase { get; private set; }
    public double ProteinsPerBase { get; private set; }
    public double FatsPerBase { get; private set; }
    public double CarbsPerBase { get; private set; }
    public double FiberPerBase { get; private set; }
    public double AlcoholPerBase { get; private set; }

    /// <summary>
    /// Количество использований в блюдах (computed column)
    /// </summary>
    public int UsageCount { get; private set; }

    public Visibility Visibility { get; private set; } = Visibility.PUBLIC;

    // Foreign keys
    public UserId UserId { get; private set; }

    // Navigation properties
    public User User { get; private set; } = null!;
    public ICollection<MealItem> MealItems { get; private set; } = new List<MealItem>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; private set; } = new List<RecipeIngredient>();

    // Конструктор для EF Core
    private Product() {
    }

    // Factory method для создания продукта
    public static Product Create(
        UserId userId,
        string name,
        MeasurementUnit baseUnit,
        double baseAmount,
        double? defaultPortionAmount,
        double caloriesPerBase,
        double proteinsPerBase,
        double fatsPerBase,
        double carbsPerBase,
        double fiberPerBase,
        double alcoholPerBase,
        string? barcode = null,
        string? brand = null,
        ProductType productType = ProductType.Unknown,
        string? category = null,
        string? description = null,
        string? comment = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        Visibility visibility = Visibility.PUBLIC) {
        var normalizedName = NormalizeRequiredName(name);
        var normalizedBaseAmount = RequirePositive(baseAmount, nameof(baseAmount));
        var normalizedDefaultPortionAmount = RequirePositive(defaultPortionAmount ?? baseAmount, nameof(defaultPortionAmount));
        var normalizedCaloriesPerBase = RequireNonNegative(caloriesPerBase, nameof(caloriesPerBase));
        var normalizedProteinsPerBase = RequireNonNegative(proteinsPerBase, nameof(proteinsPerBase));
        var normalizedFatsPerBase = RequireNonNegative(fatsPerBase, nameof(fatsPerBase));
        var normalizedCarbsPerBase = RequireNonNegative(carbsPerBase, nameof(carbsPerBase));
        var normalizedFiberPerBase = RequireNonNegative(fiberPerBase, nameof(fiberPerBase));
        var normalizedAlcoholPerBase = RequireNonNegative(alcoholPerBase, nameof(alcoholPerBase));

        var product = new Product {
            Id = ProductId.New(),
            UserId = userId,
            Name = normalizedName,
            BaseUnit = baseUnit,
            BaseAmount = normalizedBaseAmount,
            DefaultPortionAmount = normalizedDefaultPortionAmount,
            CaloriesPerBase = normalizedCaloriesPerBase,
            ProteinsPerBase = normalizedProteinsPerBase,
            FatsPerBase = normalizedFatsPerBase,
            CarbsPerBase = normalizedCarbsPerBase,
            FiberPerBase = normalizedFiberPerBase,
            AlcoholPerBase = normalizedAlcoholPerBase,
            Barcode = barcode,
            Brand = brand,
            ProductType = productType,
            Category = category,
            Description = description,
            Comment = comment,
            ImageUrl = imageUrl,
            ImageAssetId = imageAssetId,
            Visibility = visibility
        };
        product.SetCreated();
        return product;
    }

    public void Update(
        string? name = null,
        MeasurementUnit? baseUnit = null,
        double? baseAmount = null,
        double? defaultPortionAmount = null,
        double? caloriesPerBase = null,
        double? proteinsPerBase = null,
        double? fatsPerBase = null,
        double? carbsPerBase = null,
        double? fiberPerBase = null,
        double? alcoholPerBase = null,
        string? barcode = null,
        string? brand = null,
        string? category = null,
        ProductType? productType = null,
        string? description = null,
        string? comment = null,
        string? imageUrl = null,
        ImageAssetId? imageAssetId = null,
        Visibility? visibility = null) {
        if (name is not null) Name = NormalizeRequiredName(name);
        if (baseUnit.HasValue) BaseUnit = baseUnit.Value;
        if (baseAmount.HasValue) BaseAmount = RequirePositive(baseAmount.Value, nameof(baseAmount));
        if (defaultPortionAmount.HasValue) DefaultPortionAmount = RequirePositive(defaultPortionAmount.Value, nameof(defaultPortionAmount));
        if (caloriesPerBase.HasValue) CaloriesPerBase = RequireNonNegative(caloriesPerBase.Value, nameof(caloriesPerBase));
        if (proteinsPerBase.HasValue) ProteinsPerBase = RequireNonNegative(proteinsPerBase.Value, nameof(proteinsPerBase));
        if (fatsPerBase.HasValue) FatsPerBase = RequireNonNegative(fatsPerBase.Value, nameof(fatsPerBase));
        if (carbsPerBase.HasValue) CarbsPerBase = RequireNonNegative(carbsPerBase.Value, nameof(carbsPerBase));
        if (fiberPerBase.HasValue) FiberPerBase = RequireNonNegative(fiberPerBase.Value, nameof(fiberPerBase));
        if (alcoholPerBase.HasValue) AlcoholPerBase = RequireNonNegative(alcoholPerBase.Value, nameof(alcoholPerBase));
        if (barcode is not null) Barcode = barcode;
        if (brand is not null) Brand = brand;
        if (productType.HasValue) ProductType = productType.Value;
        if (category is not null) Category = category;
        if (description is not null) Description = description;
        if (comment is not null) Comment = comment;
        if (imageUrl is not null) ImageUrl = imageUrl;
        if (imageAssetId.HasValue) ImageAssetId = imageAssetId;
        if (visibility.HasValue) Visibility = visibility.Value;

        SetModified();
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Product name is required.", nameof(value));
        }

        return value.Trim();
    }

    private static double RequirePositive(double value, string paramName) {
        if (value <= 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.");
        }

        return value;
    }

    private static double RequireNonNegative(double value, string paramName) {
        if (value < 0) {
            throw new ArgumentOutOfRangeException(paramName, "Value must be non-negative.");
        }

        return value;
    }
}
