using FoodDiary.Domain.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Entities.Products;

public sealed class Product : AggregateRoot<ProductId> {
    private const double ComparisonEpsilon = 0.000001d;
    private const int NameMaxLength = 256;
    private const int BarcodeMaxLength = 128;
    private const int BrandMaxLength = 128;
    private const int CategoryMaxLength = 128;
    private const int DescriptionMaxLength = 2048;
    private const int CommentMaxLength = DomainConstants.CommentMaxLength;
    private const int ImageUrlMaxLength = DomainConstants.ImageUrlMaxLength;

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
    public int? UsdaFdcId { get; private set; }

    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;
    private readonly List<MealItem> _mealItems = [];
    private readonly List<RecipeIngredient> _recipeIngredients = [];
    public IReadOnlyCollection<MealItem> MealItems => _mealItems.AsReadOnly();
    public IReadOnlyCollection<RecipeIngredient> RecipeIngredients => _recipeIngredients.AsReadOnly();
    public UsdaFood? UsdaFood { get; private set; }

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
            Visibility = visibility
        };
        product.ApplyIdentityState(new ProductIdentityState(
            normalizedName,
            normalizedBarcode,
            normalizedBrand,
            normalizedCategory,
            productType,
            normalizedDescription,
            normalizedComment));
        product.ApplyMeasurementState(new ProductMeasurementState(
            baseUnit,
            normalizedBaseAmount,
            normalizedDefaultPortionAmount));
        product.ApplyMediaState(new ProductMediaState(
            normalizedImageUrl,
            imageAssetId));
        product.ApplyNutrition(nutrition);
        product.SetCreated();
        return product;
    }

    public void UpdateCoreIdentity(
        string? name = null,
        string? barcode = null,
        bool clearBarcode = false,
        string? brand = null,
        bool clearBrand = false,
        ProductType? productType = null) {
        var normalizedBarcode = NormalizeOptionalText(barcode, BarcodeMaxLength, nameof(barcode));
        var normalizedBrand = NormalizeOptionalText(brand, BrandMaxLength, nameof(brand));

        EnsureClearConflict(clearBarcode, normalizedBarcode, nameof(clearBarcode), nameof(barcode));
        EnsureClearConflict(clearBrand, normalizedBrand, nameof(clearBrand), nameof(brand));

        var state = GetIdentityState();

        if (name is not null) {
            state = state with { Name = NormalizeRequiredName(name) };
        }

        if (clearBarcode) {
            state = state with { Barcode = null };
        } else if (barcode is not null) {
            state = state with { Barcode = normalizedBarcode };
        }

        if (clearBrand) {
            state = state with { Brand = null };
        } else if (brand is not null) {
            state = state with { Brand = normalizedBrand };
        }

        if (productType.HasValue) {
            state = state with { ProductType = productType.Value };
        }

        ApplyIdentityStateIfChanged(state);
    }

    public void UpdateDescriptiveIdentity(
        string? category = null,
        bool clearCategory = false,
        string? description = null,
        bool clearDescription = false,
        string? comment = null,
        bool clearComment = false) {
        var normalizedCategory = NormalizeOptionalText(category, CategoryMaxLength, nameof(category));
        var normalizedDescription = NormalizeOptionalText(description, DescriptionMaxLength, nameof(description));
        var normalizedComment = NormalizeOptionalText(comment, CommentMaxLength, nameof(comment));

        EnsureClearConflict(clearCategory, normalizedCategory, nameof(clearCategory), nameof(category));
        EnsureClearConflict(clearDescription, normalizedDescription, nameof(clearDescription), nameof(description));
        EnsureClearConflict(clearComment, normalizedComment, nameof(clearComment), nameof(comment));

        var state = GetIdentityState();

        if (clearCategory) {
            state = state with { Category = null };
        } else if (category is not null) {
            state = state with { Category = normalizedCategory };
        }

        if (clearDescription) {
            state = state with { Description = null };
        } else if (description is not null) {
            state = state with { Description = normalizedDescription };
        }

        if (clearComment) {
            state = state with { Comment = null };
        } else if (comment is not null) {
            state = state with { Comment = normalizedComment };
        }

        ApplyIdentityStateIfChanged(state);
    }

    public void UpdateIdentity(ProductIdentityUpdate update) {
        UpdateCoreIdentity(
            update.Name,
            update.Barcode,
            update.ClearBarcode,
            update.Brand,
            update.ClearBrand,
            update.ProductType);
        UpdateDescriptiveIdentity(
            update.Category,
            update.ClearCategory,
            update.Description,
            update.ClearDescription,
            update.Comment,
            update.ClearComment);
    }

    public void UpdateMeasurement(
        MeasurementUnit? baseUnit = null,
        double? baseAmount = null,
        double? defaultPortionAmount = null) {
        var state = GetMeasurementState();
        var changed = false;
        var targetUnit = baseUnit ?? state.BaseUnit;
        var targetBaseAmount = state.BaseAmount;

        if (baseAmount.HasValue) {
            targetBaseAmount = NormalizeBaseAmount(targetUnit, baseAmount.Value, nameof(baseAmount));
        } else if (baseUnit.HasValue) {
            targetBaseAmount = GetCanonicalBaseAmount(targetUnit);
        }

        if (baseUnit.HasValue && state.BaseUnit != targetUnit) {
            state = state with { BaseUnit = targetUnit };
            changed = true;
        }

        if (!AreClose(state.BaseAmount, targetBaseAmount)) {
            state = state with { BaseAmount = targetBaseAmount };
            changed = true;
        }

        if (defaultPortionAmount.HasValue) {
            var normalizedDefaultPortionAmount = RequirePositive(defaultPortionAmount.Value, nameof(defaultPortionAmount));
            if (!AreClose(state.DefaultPortionAmount, normalizedDefaultPortionAmount)) {
                state = state with { DefaultPortionAmount = normalizedDefaultPortionAmount };
                changed = true;
            }
        }

        if (changed) {
            ApplyMeasurementState(state);
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

        var state = GetMediaState();
        var changed = false;

        if (clearImageUrl) {
            if (state.ImageUrl is not null) {
                state = state with { ImageUrl = null };
                changed = true;
            }
        } else if (imageUrl is not null && !string.Equals(state.ImageUrl, normalizedImageUrl, StringComparison.Ordinal)) {
            state = state with { ImageUrl = normalizedImageUrl };
            changed = true;
        }

        if (clearImageAssetId) {
            if (state.ImageAssetId is not null) {
                state = state with { ImageAssetId = null };
                changed = true;
            }
        } else if (imageAssetId.HasValue && state.ImageAssetId != imageAssetId) {
            state = state with { ImageAssetId = imageAssetId };
            changed = true;
        }

        if (changed) {
            ApplyMediaState(state);
            SetModified();
        }
    }

    public void LinkToUsdaFood(int fdcId) {
        if (fdcId <= 0) {
            throw new ArgumentOutOfRangeException(nameof(fdcId), "USDA FDC ID must be positive.");
        }

        if (UsdaFdcId == fdcId) {
            return;
        }

        UsdaFdcId = fdcId;
        SetModified();
    }

    public void UnlinkUsdaFood() {
        if (UsdaFdcId is null) {
            return;
        }

        UsdaFdcId = null;
        SetModified();
    }

    public FoodQualityScore GetQualityScore() {
        return FoodQualityScore.Calculate(
            CaloriesPerBase, ProteinsPerBase, FatsPerBase,
            CarbsPerBase, FiberPerBase, AlcoholPerBase, ProductType);
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

    private ProductMeasurementState GetMeasurementState() {
        return new ProductMeasurementState(
            BaseUnit,
            BaseAmount,
            DefaultPortionAmount);
    }

    private void ApplyMeasurementState(ProductMeasurementState state) {
        BaseUnit = state.BaseUnit;
        BaseAmount = state.BaseAmount;
        DefaultPortionAmount = state.DefaultPortionAmount;
    }

    private ProductMediaState GetMediaState() {
        return new ProductMediaState(
            ImageUrl,
            ImageAssetId);
    }

    private void ApplyMediaState(ProductMediaState state) {
        ImageUrl = state.ImageUrl;
        ImageAssetId = state.ImageAssetId;
    }

    private ProductIdentityState GetIdentityState() {
        return new ProductIdentityState(
            Name,
            Barcode,
            Brand,
            Category,
            ProductType,
            Description,
            Comment);
    }

    private void ApplyIdentityStateIfChanged(ProductIdentityState updatedState) {
        if (GetIdentityState() == updatedState) {
            return;
        }

        ApplyIdentityState(updatedState);
        SetModified();
    }

    private void ApplyIdentityState(ProductIdentityState state) {
        Name = state.Name;
        Barcode = state.Barcode;
        Brand = state.Brand;
        Category = state.Category;
        ProductType = state.ProductType;
        Description = state.Description;
        Comment = state.Comment;
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
