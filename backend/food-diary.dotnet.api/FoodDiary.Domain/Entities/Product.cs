using FoodDiary.Domain.Common;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities;

/// <summary>
/// Базовый продукт питания - корень агрегата
/// Представляет простой продукт (например, с штрихкодом из магазина)
/// </summary>
public class Product : AggregateRoot<ProductId>
{
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Brand { get; private set; }
    public string? Category { get; private set; }
    public string? Description { get; private set; }
    public string? ImageUrl { get; private set; }
    public MeasurementUnit BaseUnit { get; private set; }
    public double BaseAmount { get; private set; }
    public double CaloriesPerBase { get; private set; }
    public double ProteinsPerBase { get; private set; }
    public double FatsPerBase { get; private set; }
    public double CarbsPerBase { get; private set; }
    public double FiberPerBase { get; private set; }

    /// <summary>
    /// Количество использований в блюдах (computed column)
    /// </summary>
    public int UsageCount { get; private set; }

    public Visibility Visibility { get; private set; } = Visibility.PUBLIC;

    // Foreign keys
    public UserId UserId { get; private set; }

    // Navigation properties
    public virtual User User { get; private set; } = null!;
    public virtual ICollection<MealItem> MealItems { get; private set; } = new List<MealItem>();
    public virtual ICollection<RecipeIngredient> RecipeIngredients { get; private set; } = new List<RecipeIngredient>();

    // Конструктор для EF Core
    private Product() { }

    // Factory method для создания продукта
    public static Product Create(
        UserId userId,
        string name,
        MeasurementUnit baseUnit,
        double baseAmount,
        double caloriesPerBase,
        double proteinsPerBase,
        double fatsPerBase,
        double carbsPerBase,
        double fiberPerBase,
        string? barcode = null,
        string? brand = null,
        string? category = null,
        string? description = null,
        string? imageUrl = null,
        Visibility visibility = Visibility.PUBLIC)
    {
        var product = new Product
        {
            Id = ProductId.New(),
            UserId = userId,
            Name = name,
            BaseUnit = baseUnit,
            BaseAmount = baseAmount,
            CaloriesPerBase = caloriesPerBase,
            ProteinsPerBase = proteinsPerBase,
            FatsPerBase = fatsPerBase,
            CarbsPerBase = carbsPerBase,
            FiberPerBase = fiberPerBase,
            Barcode = barcode,
            Brand = brand,
            Category = category,
            Description = description,
            ImageUrl = imageUrl,
            Visibility = visibility
        };
        product.SetCreated();
        return product;
    }

    public void Update(
        string? name = null,
        MeasurementUnit? baseUnit = null,
        double? baseAmount = null,
        double? caloriesPerBase = null,
        double? proteinsPerBase = null,
        double? fatsPerBase = null,
        double? carbsPerBase = null,
        double? fiberPerBase = null,
        string? barcode = null,
        string? brand = null,
        string? category = null,
        string? description = null,
        string? imageUrl = null,
        Visibility? visibility = null)
    {
        if (name is not null) Name = name;
        if (baseUnit.HasValue) BaseUnit = baseUnit.Value;
        if (baseAmount.HasValue) BaseAmount = baseAmount.Value;
        if (caloriesPerBase.HasValue) CaloriesPerBase = caloriesPerBase.Value;
        if (proteinsPerBase.HasValue) ProteinsPerBase = proteinsPerBase.Value;
        if (fatsPerBase.HasValue) FatsPerBase = fatsPerBase.Value;
        if (carbsPerBase.HasValue) CarbsPerBase = carbsPerBase.Value;
        if (fiberPerBase.HasValue) FiberPerBase = fiberPerBase.Value;
        if (barcode is not null) Barcode = barcode;
        if (brand is not null) Brand = brand;
        if (category is not null) Category = category;
        if (description is not null) Description = description;
        if (imageUrl is not null) ImageUrl = imageUrl;
        if (visibility.HasValue) Visibility = visibility.Value;

        SetModified();
    }
}
