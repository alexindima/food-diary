using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Entities.Products;

public sealed class Product : AggregateRoot<ProductId> {
    private const double ComparisonEpsilon = 0.000001d;
    private const int NameMaxLength = 256;
    private const int BarcodeMaxLength = 128;
    private const int BrandMaxLength = 128;
    private const int CategoryMaxLength = 128;
    private const int DescriptionMaxLength = 2048;
    private const int CommentMaxLength = 2048;
    private const int ImageUrlMaxLength = 2048;

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
    public double DefaultPortionAmount { get; private set; }
    public double CaloriesPerBase { get; private set; }
    public double ProteinsPerBase { get; private set; }
    public double FatsPerBase { get; private set; }
    public double CarbsPerBase { get; private set; }
    public double FiberPerBase { get; private set; }
    public double AlcoholPerBase { get; private set; }
    public int UsageCount { get; private set; }
    public Visibility Visibility { get; private set; } = Visibility.Public;

    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;
    public ICollection<MealItem> MealItems { get; private set; } = new List<MealItem>();
    public ICollection<RecipeIngredient> RecipeIngredients { get; private set; } = new List<RecipeIngredient>();

    private Product() {
    }

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
        Visibility visibility = Visibility.Public) {
        EnsureUserId(userId);
        var normalizedName = NormalizeRequiredName(name);
        var normalizedBaseAmount = NormalizeBaseAmount(baseUnit, baseAmount, nameof(baseAmount));
        var normalizedDefaultPortionAmount = RequirePositive(defaultPortionAmount ?? normalizedBaseAmount, nameof(defaultPortionAmount));
        var nutrition = ProductNutrition.Create(
            caloriesPerBase,
            proteinsPerBase,
            fatsPerBase,
            carbsPerBase,
            fiberPerBase,
            alcoholPerBase);
        var normalizedBarcode = NormalizeOptionalText(barcode, BarcodeMaxLength, nameof(barcode));
        var normalizedBrand = NormalizeOptionalText(brand, BrandMaxLength, nameof(brand));
        var normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));
        var normalizedDescription = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
        var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));
        var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));

        var product = new Product {
            Id = ProductId.New(),
            UserId = userId,
            Name = normalizedName,
            BaseUnit = baseUnit,
            BaseAmount = normalizedBaseAmount,
            DefaultPortionAmount = normalizedDefaultPortionAmount,
            CaloriesPerBase = nutrition.CaloriesPerBase,
            ProteinsPerBase = nutrition.ProteinsPerBase,
            FatsPerBase = nutrition.FatsPerBase,
            CarbsPerBase = nutrition.CarbsPerBase,
            FiberPerBase = nutrition.FiberPerBase,
            AlcoholPerBase = nutrition.AlcoholPerBase,
            Barcode = normalizedBarcode,
            Brand = normalizedBrand,
            ProductType = productType,
            Category = normalizedCategory,
            Description = normalizedDescription,
            Comment = normalizedComment,
            ImageUrl = normalizedImageUrl,
            ImageAssetId = imageAssetId,
            Visibility = visibility
        };
        product.SetCreated();
        return product;
    }

    public void UpdateIdentity(
        string? name = null,
        string? barcode = null,
        bool clearBarcode = false,
        string? brand = null,
        bool clearBrand = false,
        string? category = null,
        bool clearCategory = false,
        ProductType? productType = null,
        string? description = null,
        bool clearDescription = false,
        string? comment = null,
        bool clearComment = false) {
        var normalizedBarcode = NormalizeOptionalText(barcode, BarcodeMaxLength, nameof(barcode));
        var normalizedBrand = NormalizeOptionalText(brand, BrandMaxLength, nameof(brand));
        var normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));
        var normalizedDescription = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
        var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));

        EnsureClearConflict(clearBarcode, normalizedBarcode, nameof(clearBarcode), nameof(barcode));
        EnsureClearConflict(clearBrand, normalizedBrand, nameof(clearBrand), nameof(brand));
        EnsureClearConflict(clearCategory, normalizedCategory, nameof(clearCategory), nameof(category));
        EnsureClearConflict(clearDescription, normalizedDescription, nameof(clearDescription), nameof(description));
        EnsureClearConflict(clearComment, normalizedComment, nameof(clearComment), nameof(comment));

        var changed = false;

        if (name is not null) {
            var normalizedName = NormalizeRequiredName(name);
            if (!string.Equals(Name, normalizedName, StringComparison.Ordinal)) {
                Name = normalizedName;
                changed = true;
            }
        }

        if (clearBarcode) {
            if (Barcode is not null) {
                Barcode = null;
                changed = true;
            }
        }
        else if (barcode is not null && !string.Equals(Barcode, normalizedBarcode, StringComparison.Ordinal)) {
            Barcode = normalizedBarcode;
            changed = true;
        }

        if (clearBrand) {
            if (Brand is not null) {
                Brand = null;
                changed = true;
            }
        }
        else if (brand is not null && !string.Equals(Brand, normalizedBrand, StringComparison.Ordinal)) {
            Brand = normalizedBrand;
            changed = true;
        }

        if (productType.HasValue && ProductType != productType.Value) {
            ProductType = productType.Value;
            changed = true;
        }

        if (clearCategory) {
            if (Category is not null) {
                Category = null;
                changed = true;
            }
        }
        else if (category is not null && !string.Equals(Category, normalizedCategory, StringComparison.Ordinal)) {
            Category = normalizedCategory;
            changed = true;
        }

        if (clearDescription) {
            if (Description is not null) {
                Description = null;
                changed = true;
            }
        }
        else if (description is not null && !string.Equals(Description, normalizedDescription, StringComparison.Ordinal)) {
            Description = normalizedDescription;
            changed = true;
        }

        if (clearComment) {
            if (Comment is not null) {
                Comment = null;
                changed = true;
            }
        }
        else if (comment is not null && !string.Equals(Comment, normalizedComment, StringComparison.Ordinal)) {
            Comment = normalizedComment;
            changed = true;
        }

        if (changed) {
            SetModified();
        }
    }

    public void UpdateMeasurement(
        MeasurementUnit? baseUnit = null,
        double? baseAmount = null,
        double? defaultPortionAmount = null) {
        var changed = false;
        var targetUnit = baseUnit ?? BaseUnit;
        var targetBaseAmount = BaseAmount;

        if (baseAmount.HasValue) {
            targetBaseAmount = NormalizeBaseAmount(targetUnit, baseAmount.Value, nameof(baseAmount));
        }
        else if (baseUnit.HasValue) {
            targetBaseAmount = GetCanonicalBaseAmount(targetUnit);
        }

        if (baseUnit.HasValue && BaseUnit != targetUnit) {
            BaseUnit = targetUnit;
            changed = true;
        }

        if (!AreClose(BaseAmount, targetBaseAmount)) {
            BaseAmount = targetBaseAmount;
            changed = true;
        }

        if (defaultPortionAmount.HasValue) {
            var normalizedDefaultPortionAmount = RequirePositive(defaultPortionAmount.Value, nameof(defaultPortionAmount));
            if (!AreClose(DefaultPortionAmount, normalizedDefaultPortionAmount)) {
                DefaultPortionAmount = normalizedDefaultPortionAmount;
                changed = true;
            }
        }

        if (changed) {
            SetModified();
        }
    }

    public void UpdateNutrition(
        double? caloriesPerBase = null,
        double? proteinsPerBase = null,
        double? fatsPerBase = null,
        double? carbsPerBase = null,
        double? fiberPerBase = null,
        double? alcoholPerBase = null) {
        var currentNutrition = GetNutrition();
        var updatedNutrition = currentNutrition.With(
            caloriesPerBase: caloriesPerBase,
            proteinsPerBase: proteinsPerBase,
            fatsPerBase: fatsPerBase,
            carbsPerBase: carbsPerBase,
            fiberPerBase: fiberPerBase,
            alcoholPerBase: alcoholPerBase);

        if (!currentNutrition.IsCloseTo(updatedNutrition, ComparisonEpsilon)) {
            ApplyNutrition(updatedNutrition);
            SetModified();
        }
    }

    public void UpdateMedia(
        string? imageUrl = null,
        bool clearImageUrl = false,
        ImageAssetId? imageAssetId = null,
        bool clearImageAssetId = false) {
        var normalizedImageUrl = NormalizeOptionalText(imageUrl, ImageUrlMaxLength, nameof(imageUrl));

        EnsureClearConflict(clearImageUrl, normalizedImageUrl, nameof(clearImageUrl), nameof(imageUrl));
        EnsureClearConflict(clearImageAssetId, imageAssetId, nameof(clearImageAssetId), nameof(imageAssetId));

        var changed = false;

        if (clearImageUrl) {
            if (ImageUrl is not null) {
                ImageUrl = null;
                changed = true;
            }
        }
        else if (imageUrl is not null && !string.Equals(ImageUrl, normalizedImageUrl, StringComparison.Ordinal)) {
            ImageUrl = normalizedImageUrl;
            changed = true;
        }

        if (clearImageAssetId) {
            if (ImageAssetId is not null) {
                ImageAssetId = null;
                changed = true;
            }
        }
        else if (imageAssetId.HasValue && ImageAssetId != imageAssetId) {
            ImageAssetId = imageAssetId;
            changed = true;
        }

        if (changed) {
            SetModified();
        }
    }

    public void ChangeVisibility(Visibility visibility) {
        if (Visibility == visibility) {
            return;
        }

        Visibility = visibility;

        SetModified();
    }

    private static string NormalizeRequiredName(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException("Product name is required.", nameof(value));
        }

        var normalized = value.Trim();
        return normalized.Length > NameMaxLength
            ? throw new ArgumentOutOfRangeException(nameof(value), $"Product name must be at most {NameMaxLength} characters.")
            : normalized;
    }

    private static double RequirePositive(double value, string paramName) {
        return value <= 0
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be greater than zero.")
            : value;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength, string paramName) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length > maxLength
            ? throw new ArgumentOutOfRangeException(paramName, $"Value must be at most {maxLength} characters.")
            : normalized;
    }

    private static double NormalizeBaseAmount(MeasurementUnit unit, double value, string paramName) {
        RequirePositive(value, paramName);
        var canonicalAmount = GetCanonicalBaseAmount(unit);
        return !AreClose(value, canonicalAmount)
            ? throw new ArgumentOutOfRangeException(paramName, $"Base amount for {unit} must be {canonicalAmount}.")
            : canonicalAmount;
    }

    private static double GetCanonicalBaseAmount(MeasurementUnit unit) {
        return unit == MeasurementUnit.Pcs ? 1d : 100d;
    }

    private static bool AreClose(double left, double right) {
        return Math.Abs(left - right) <= ComparisonEpsilon;
    }

    private ProductNutrition GetNutrition() {
        return new ProductNutrition(
            CaloriesPerBase,
            ProteinsPerBase,
            FatsPerBase,
            CarbsPerBase,
            FiberPerBase,
            AlcoholPerBase);
    }

    private void ApplyNutrition(ProductNutrition nutrition) {
        CaloriesPerBase = nutrition.CaloriesPerBase;
        ProteinsPerBase = nutrition.ProteinsPerBase;
        FatsPerBase = nutrition.FatsPerBase;
        CarbsPerBase = nutrition.CarbsPerBase;
        FiberPerBase = nutrition.FiberPerBase;
        AlcoholPerBase = nutrition.AlcoholPerBase;
    }

    private static void EnsureUserId(UserId userId) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }
    }

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : class {
        if (clear && value is not null) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }

    private static void EnsureClearConflict<T>(bool clear, T? value, string clearParamName, string valueParamName)
        where T : struct {
        if (clear && value.HasValue) {
            throw new ArgumentException($"{clearParamName} cannot be true when {valueParamName} is provided.", clearParamName);
        }
    }
}
