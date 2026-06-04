using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Commands.UpdateProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using System.Reflection;

namespace FoodDiary.Application.Tests.Products;

[ExcludeFromCodeCoverage]
public class ProductsValidatorTests {
    private static CreateProductCommand ValidCreateProduct(Guid? userId = null) =>
        new(userId ?? Guid.NewGuid(), null, "Chicken", null, "Other", null, null, null, null, null,
            "g", 100, 100, 165, 31, 3.6, 0, 0, 0, "Private");

    private static UpdateProductCommand ValidUpdateProduct(Guid? userId = null, Guid? productId = null) =>
        new(
            userId ?? Guid.NewGuid(),
            productId ?? Guid.NewGuid(),
            Barcode: null,
            ClearBarcode: false,
            Name: "Chicken",
            Brand: null,
            ClearBrand: false,
            ProductType: "Other",
            Category: null,
            ClearCategory: false,
            Description: null,
            ClearDescription: false,
            Comment: null,
            ClearComment: false,
            ImageUrl: null,
            ClearImageUrl: false,
            ImageAssetId: null,
            ClearImageAssetId: false,
            BaseUnit: "g",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 165,
            ProteinsPerBase: 31,
            FatsPerBase: 3.6,
            CarbsPerBase: 0,
            FiberPerBase: 0,
            AlcoholPerBase: 0,
            Visibility: "Private");

    // â”€â”€ CreateProduct â”€â”€

    [Fact]
    public async Task CreateProduct_WithNullUserId_HasError() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct(userId: null) with { UserId = null });
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_HasError() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { Name = "" });
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidUnit_HasError() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { BaseUnit = "invalid" });
        result.ShouldHaveValidationErrorFor(c => c.BaseUnit);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidProductType_HasError() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { ProductType = "invalid" });
        result.ShouldHaveValidationErrorFor(c => c.ProductType);
    }

    [Fact]
    public async Task CreateProduct_WithNegativeCalories_HasError() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { CaloriesPerBase = -1 });
        result.ShouldHaveValidationErrorFor(c => c.CaloriesPerBase);
    }

    [Fact]
    public async Task CreateProduct_WithZeroBaseAmount_HasError() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { BaseAmount = 0 });
        result.ShouldHaveValidationErrorFor(c => c.BaseAmount);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_NoErrors() {
        var result = await new CreateProductCommandValidator().TestValidateAsync(ValidCreateProduct());
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ DuplicateProduct â”€â”€

    [Fact]
    public async Task DuplicateProduct_WithNullUserId_HasError() {
        var result = await new DuplicateProductCommandValidator().TestValidateAsync(
            new DuplicateProductCommand(null, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task DuplicateProduct_WithEmptyProductId_HasError() {
        var result = await new DuplicateProductCommandValidator().TestValidateAsync(
            new DuplicateProductCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ProductId);
    }

    // â”€â”€ GetProductById â”€â”€

    [Fact]
    public async Task GetProductById_WithNullUserId_HasError() {
        var result = await new GetProductByIdQueryValidator().TestValidateAsync(
            new GetProductByIdQuery(null, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetProductById_WithEmptyProductId_HasError() {
        var result = await new GetProductByIdQueryValidator().TestValidateAsync(
            new GetProductByIdQuery(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ProductId);
    }

    // â”€â”€ GetProducts â”€â”€

    [Fact]
    public async Task GetProducts_WithZeroPage_HasError() {
        var result = await new GetProductsQueryValidator().TestValidateAsync(
            new GetProductsQuery(Guid.NewGuid(), 0, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Page);
    }

    [Fact]
    public async Task GetProducts_WithZeroLimit_HasError() {
        var result = await new GetProductsQueryValidator().TestValidateAsync(
            new GetProductsQuery(Guid.NewGuid(), 1, 0, null, false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    // â”€â”€ GetProductsWithRecent â”€â”€

    [Fact]
    public async Task GetProductsOverview_WithNullUserId_HasError() {
        var result = await new GetProductsOverviewQueryValidator().TestValidateAsync(
            new GetProductsOverviewQuery(null, 1, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // â”€â”€ GetRecentProducts â”€â”€

    [Fact]
    public async Task GetRecentProducts_WithNullUserId_HasError() {
        var result = await new GetRecentProductsQueryValidator().TestValidateAsync(
            new GetRecentProductsQuery(null, 10, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // UpdateProduct

    [Fact]
    public async Task UpdateProduct_WithNullUserId_HasInvalidTokenError() {
        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
            ValidUpdateProduct(userId: null) with { UserId = null });

        result.ShouldHaveValidationErrorFor(c => c.UserId)
            .WithErrorCode("Authentication.InvalidToken");
    }

    [Fact]
    public async Task UpdateProduct_WithEmptyProductId_HasRequiredError() {
        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
            ValidUpdateProduct(productId: Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.ProductId)
            .WithErrorCode("Validation.Required");
    }

    [Theory]
    [InlineData("invalid", "Other", "Private", "BaseUnit")]
    [InlineData("g", "invalid", "Private", "ProductType")]
    [InlineData("g", "Other", "invalid", "Visibility")]
    public async Task UpdateProduct_WithInvalidEnumValue_HasValidationError(
        string baseUnit,
        string productType,
        string visibility,
        string propertyName) {
        var product = CreateProduct();
        var command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = baseUnit,
            ProductType = productType,
            Visibility = visibility
        };

        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        Assert.Contains(result.Errors, error =>
            string.Equals(error.PropertyName, propertyName, StringComparison.Ordinal) &&
            string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(0, 100, 165, 31, 3.6, 0, 0, 0, "BaseAmount")]
    [InlineData(100, 0, 165, 31, 3.6, 0, 0, 0, "DefaultPortionAmount")]
    [InlineData(100, 100, -1, 31, 3.6, 0, 0, 0, "CaloriesPerBase")]
    [InlineData(100, 100, 165, -1, 3.6, 0, 0, 0, "ProteinsPerBase")]
    [InlineData(100, 100, 165, 31, -1, 0, 0, 0, "FatsPerBase")]
    [InlineData(100, 100, 165, 31, 3.6, -1, 0, 0, "CarbsPerBase")]
    [InlineData(100, 100, 165, 31, 3.6, 0, -1, 0, "FiberPerBase")]
    [InlineData(100, 100, 165, 31, 3.6, 0, 0, -1, "AlcoholPerBase")]
    public async Task UpdateProduct_WithInvalidNutritionValue_HasValidationError(
        double baseAmount,
        double defaultPortionAmount,
        double calories,
        double proteins,
        double fats,
        double carbs,
        double fiber,
        double alcohol,
        string propertyName) {
        var product = CreateProduct();
        var command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseAmount = baseAmount,
            DefaultPortionAmount = defaultPortionAmount,
            CaloriesPerBase = calories,
            ProteinsPerBase = proteins,
            FatsPerBase = fats,
            CarbsPerBase = carbs,
            FiberPerBase = fiber,
            AlcoholPerBase = alcohol
        };

        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        Assert.Contains(result.Errors, error =>
            string.Equals(error.PropertyName, propertyName, StringComparison.Ordinal) &&
            string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdateProduct_WithClearFlagsAndValues_HasValidationErrors() {
        var product = CreateProduct();
        var command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            Barcode = "123",
            ClearBarcode = true,
            Brand = "Brand",
            ClearBrand = true,
            Category = "Category",
            ClearCategory = true,
            Description = "Description",
            ClearDescription = true,
            Comment = "Comment",
            ClearComment = true,
            ImageUrl = "https://cdn.test/image.png",
            ClearImageUrl = true,
            ImageAssetId = Guid.NewGuid(),
            ClearImageAssetId = true
        };

        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        Assert.Equal(7, result.Errors.Count(error => string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task UpdateProduct_WhenProductIsMissing_HasProductNotFoundError() {
        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(ValidUpdateProduct());

        result.ShouldHaveValidationErrorFor(c => c.ProductId)
            .WithErrorCode("Product.NotFound");
    }

    [Fact]
    public async Task UpdateProduct_WhenProductIsAlreadyUsed_HasValidationError() {
        var product = CreateProduct();
        SetProductUsageCollections(product, mealItemsCount: 1, recipeIngredientsCount: 1);

        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(
            ValidUpdateProduct(product.UserId.Value, product.Id.Value));

        result.ShouldHaveValidationErrorFor(c => c.ProductId)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task UpdateProduct_WithEditableProduct_HasNoValidationErrors() {
        var product = CreateProduct();

        var result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(
            ValidUpdateProduct(product.UserId.Value, product.Id.Value));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static Product CreateProduct() =>
        Product.Create(
            UserId.New(),
            name: "Chicken",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 100,
            caloriesPerBase: 165,
            proteinsPerBase: 31,
            fatsPerBase: 3.6,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

    private static void SetProductUsageCollections(Product product, int mealItemsCount, int recipeIngredientsCount) {
        var mealItems = Enumerable.Range(0, mealItemsCount)
            .Select(static _ => (FoodDiary.Domain.Entities.Meals.MealItem)null!)
            .ToList();
        var recipeIngredients = Enumerable.Range(0, recipeIngredientsCount)
            .Select(static _ => (FoodDiary.Domain.Entities.Recipes.RecipeIngredient)null!)
            .ToList();

        typeof(Product)
            .GetField("_mealItems", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(product, mealItems);
        typeof(Product)
            .GetField("_recipeIngredients", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(product, recipeIngredients);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProductRepositoryStub(Product? product = null) : IProductRepository {
        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(product is not null && product.Id == id && product.UserId == userId ? product : null);

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
