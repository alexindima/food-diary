using FluentValidation.TestHelper;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DeleteProduct;
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
        new(userId ?? Guid.NewGuid(), Barcode: null, "Chicken", Brand: null, "Other", Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
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
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct(userId: null) with { UserId = null });
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateProduct_WithEmptyName_HasError() {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { Name = "" });
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidUnit_HasError() {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { BaseUnit = "invalid" });
        result.ShouldHaveValidationErrorFor(c => c.BaseUnit);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidProductType_HasError() {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { ProductType = "invalid" });
        result.ShouldHaveValidationErrorFor(c => c.ProductType);
    }

    [Fact]
    public async Task CreateProduct_WithNegativeCalories_HasError() {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { CaloriesPerBase = -1 });
        result.ShouldHaveValidationErrorFor(c => c.CaloriesPerBase);
    }

    [Fact]
    public async Task CreateProduct_WithZeroBaseAmount_HasError() {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with { BaseAmount = 0 });
        result.ShouldHaveValidationErrorFor(c => c.BaseAmount);
    }

    [Theory]
    [InlineData("g", Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData("ml", Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData("pcs", Product.MaxPieceDefaultPortionAmount + 1)]
    public async Task CreateProduct_WithDefaultPortionAmountAboveUnitLimit_HasError(string baseUnit, double defaultPortionAmount) {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with {
                BaseUnit = baseUnit,
                BaseAmount = string.Equals(baseUnit, "pcs", StringComparison.OrdinalIgnoreCase) ? 1 : 100,
                DefaultPortionAmount = defaultPortionAmount,
            });

        result.ShouldHaveValidationErrorFor(c => c.DefaultPortionAmount)
            .WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [InlineData("g", Product.MaxWeightOrVolumeCaloriesPerBase + 1)]
    [InlineData("ml", Product.MaxWeightOrVolumeCaloriesPerBase + 1)]
    [InlineData("pcs", Product.MaxPieceCaloriesPerBase + 1)]
    public async Task CreateProduct_WithCaloriesAboveUnitLimit_HasError(string baseUnit, double caloriesPerBase) {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with {
                BaseUnit = baseUnit,
                BaseAmount = string.Equals(baseUnit, "pcs", StringComparison.OrdinalIgnoreCase) ? 1 : 100,
                CaloriesPerBase = caloriesPerBase,
            });

        result.ShouldHaveValidationErrorFor(c => c.CaloriesPerBase)
            .WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [InlineData("g", Product.MaxWeightOrVolumeNutrientPerBase + 1)]
    [InlineData("ml", Product.MaxWeightOrVolumeNutrientPerBase + 1)]
    [InlineData("pcs", Product.MaxPieceNutrientPerBase + 1)]
    public async Task CreateProduct_WithNutrientAboveUnitLimit_HasError(string baseUnit, double nutrientPerBase) {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(
            ValidCreateProduct() with {
                BaseUnit = baseUnit,
                BaseAmount = string.Equals(baseUnit, "pcs", StringComparison.OrdinalIgnoreCase) ? 1 : 100,
                ProteinsPerBase = nutrientPerBase,
            });

        result.ShouldHaveValidationErrorFor(c => c.ProteinsPerBase)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task CreateProduct_WithValidData_NoErrors() {
        TestValidationResult<CreateProductCommand> result = await new CreateProductCommandValidator().TestValidateAsync(ValidCreateProduct());
        result.ShouldNotHaveAnyValidationErrors();
    }

    // â”€â”€ DuplicateProduct â”€â”€

    [Fact]
    public async Task DuplicateProduct_WithNullUserId_HasError() {
        TestValidationResult<DuplicateProductCommand> result = await new DuplicateProductCommandValidator().TestValidateAsync(
            new DuplicateProductCommand(UserId: null, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task DuplicateProduct_WithEmptyProductId_HasError() {
        TestValidationResult<DuplicateProductCommand> result = await new DuplicateProductCommandValidator().TestValidateAsync(
            new DuplicateProductCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ProductId);
    }

    // â”€â”€ GetProductById â”€â”€

    [Fact]
    public async Task GetProductById_WithNullUserId_HasError() {
        TestValidationResult<GetProductByIdQuery> result = await new GetProductByIdQueryValidator().TestValidateAsync(
            new GetProductByIdQuery(UserId: null, Guid.NewGuid()));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetProductById_WithEmptyProductId_HasError() {
        TestValidationResult<GetProductByIdQuery> result = await new GetProductByIdQueryValidator().TestValidateAsync(
            new GetProductByIdQuery(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ProductId);
    }

    // â”€â”€ GetProducts â”€â”€

    [Fact]
    public async Task GetProducts_WithZeroPage_HasError() {
        TestValidationResult<GetProductsQuery> result = await new GetProductsQueryValidator().TestValidateAsync(
            new GetProductsQuery(Guid.NewGuid(), 0, 10, Search: null, IncludePublic: false));
        result.ShouldHaveValidationErrorFor(c => c.Page);
    }

    [Fact]
    public async Task GetProducts_WithZeroLimit_HasError() {
        TestValidationResult<GetProductsQuery> result = await new GetProductsQueryValidator().TestValidateAsync(
            new GetProductsQuery(Guid.NewGuid(), 1, 0, Search: null, IncludePublic: false));
        result.ShouldHaveValidationErrorFor(c => c.Limit);
    }

    // â”€â”€ GetProductsWithRecent â”€â”€

    [Fact]
    public async Task GetProductsOverview_WithNullUserId_HasError() {
        TestValidationResult<GetProductsOverviewQuery> result = await new GetProductsOverviewQueryValidator().TestValidateAsync(
            new GetProductsOverviewQuery(UserId: null, 1, 10, Search: null, IncludePublic: false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // â”€â”€ GetRecentProducts â”€â”€

    [Fact]
    public async Task GetRecentProducts_WithNullUserId_HasError() {
        TestValidationResult<GetRecentProductsQuery> result = await new GetRecentProductsQueryValidator().TestValidateAsync(
            new GetRecentProductsQuery(UserId: null, 10, IncludePublic: false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // UpdateProduct

    [Fact]
    public async Task UpdateProduct_WithNullUserId_HasInvalidTokenError() {
        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
            ValidUpdateProduct(userId: null) with { UserId = null });

        result.ShouldHaveValidationErrorFor(c => c.UserId)
            .WithErrorCode("Authentication.InvalidToken");
    }

    [Fact]
    public async Task UpdateProduct_WithEmptyProductId_HasRequiredError() {
        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
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
        Product product = CreateProduct();
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = baseUnit,
            ProductType = productType,
            Visibility = visibility,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

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
        Product product = CreateProduct();
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseAmount = baseAmount,
            DefaultPortionAmount = defaultPortionAmount,
            CaloriesPerBase = calories,
            ProteinsPerBase = proteins,
            FatsPerBase = fats,
            CarbsPerBase = carbs,
            FiberPerBase = fiber,
            AlcoholPerBase = alcohol,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        Assert.Contains(result.Errors, error =>
            string.Equals(error.PropertyName, propertyName, StringComparison.Ordinal) &&
            string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("g", Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData("ml", Product.MaxWeightOrVolumeDefaultPortionAmount + 1)]
    [InlineData("pcs", Product.MaxPieceDefaultPortionAmount + 1)]
    public async Task UpdateProduct_WithDefaultPortionAmountAboveUnitLimit_HasValidationError(string baseUnit, double defaultPortionAmount) {
        Product product = CreateProduct();
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = baseUnit,
            BaseAmount = string.Equals(baseUnit, "pcs", StringComparison.OrdinalIgnoreCase) ? 1 : 100,
            DefaultPortionAmount = defaultPortionAmount,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.DefaultPortionAmount)
            .WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [InlineData("g", Product.MaxWeightOrVolumeCaloriesPerBase + 1)]
    [InlineData("ml", Product.MaxWeightOrVolumeCaloriesPerBase + 1)]
    [InlineData("pcs", Product.MaxPieceCaloriesPerBase + 1)]
    public async Task UpdateProduct_WithCaloriesAboveUnitLimit_HasValidationError(string baseUnit, double caloriesPerBase) {
        Product product = CreateProduct();
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = baseUnit,
            BaseAmount = string.Equals(baseUnit, "pcs", StringComparison.OrdinalIgnoreCase) ? 1 : 100,
            CaloriesPerBase = caloriesPerBase,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CaloriesPerBase)
            .WithErrorCode("Validation.Invalid");
    }

    [Theory]
    [InlineData("g", Product.MaxWeightOrVolumeNutrientPerBase + 1)]
    [InlineData("ml", Product.MaxWeightOrVolumeNutrientPerBase + 1)]
    [InlineData("pcs", Product.MaxPieceNutrientPerBase + 1)]
    public async Task UpdateProduct_WithNutrientAboveUnitLimit_HasValidationError(string baseUnit, double nutrientPerBase) {
        Product product = CreateProduct();
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = baseUnit,
            BaseAmount = string.Equals(baseUnit, "pcs", StringComparison.OrdinalIgnoreCase) ? 1 : 100,
            ProteinsPerBase = nutrientPerBase,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.ProteinsPerBase)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task UpdateProduct_ToStricterUnitWithExistingNutritionAboveLimit_HasValidationError() {
        var product = Product.Create(
            UserId.New(),
            name: "Large piece",
            baseUnit: MeasurementUnit.Pcs,
            baseAmount: 1,
            defaultPortionAmount: 1,
            caloriesPerBase: Product.MaxWeightOrVolumeCaloriesPerBase + 1,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = "g",
            BaseAmount = 100,
            CaloriesPerBase = null,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.CaloriesPerBase)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task UpdateProduct_WithPieceUnitAndDefaultPortionAbovePieceLimit_HasValidationError() {
        var product = Product.Create(
            UserId.New(),
            name: "Vitamin",
            baseUnit: MeasurementUnit.Pcs,
            baseAmount: 1,
            defaultPortionAmount: 1,
            caloriesPerBase: 1,
            proteinsPerBase: 0,
            fatsPerBase: 0,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
            BaseUnit = null,
            BaseAmount = null,
            DefaultPortionAmount = Product.MaxPieceDefaultPortionAmount + 1,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(c => c.DefaultPortionAmount)
            .WithErrorCode("Validation.Invalid");
    }

    [Fact]
    public async Task UpdateProduct_WithClearFlagsAndValues_HasValidationErrors() {
        Product product = CreateProduct();
        UpdateProductCommand command = ValidUpdateProduct(product.UserId.Value, product.Id.Value) with {
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
            ClearImageAssetId = true,
        };

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(command);

        Assert.Equal(7, result.Errors.Count(error => string.Equals(error.ErrorCode, "Validation.Invalid", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task UpdateProduct_WhenProductIsMissing_HasProductNotFoundError() {
        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(ValidUpdateProduct());

        result.ShouldHaveValidationErrorFor(c => c.ProductId)
            .WithErrorCode("Product.NotFound");
    }

    [Fact]
    public async Task UpdateProduct_WhenProductIsAlreadyUsed_HasNoValidationErrors() {
        Product product = CreateProduct();
        SetProductUsageCollections(product, mealItemsCount: 1, recipeIngredientsCount: 1);

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(
            ValidUpdateProduct(product.UserId.Value, product.Id.Value));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task UpdateProduct_WithEditableProduct_HasNoValidationErrors() {
        Product product = CreateProduct();

        TestValidationResult<UpdateProductCommand> result = await new UpdateProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(
            ValidUpdateProduct(product.UserId.Value, product.Id.Value));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteProduct_WithNullUserId_HasInvalidTokenError() {
        TestValidationResult<DeleteProductCommand> result = await new DeleteProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
            new DeleteProductCommand(UserId: null, Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.UserId)
            .WithErrorCode("Authentication.InvalidToken");
    }

    [Fact]
    public async Task DeleteProduct_WithEmptyProductId_HasRequiredError() {
        TestValidationResult<DeleteProductCommand> result = await new DeleteProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
            new DeleteProductCommand(Guid.NewGuid(), Guid.Empty));

        result.ShouldHaveValidationErrorFor(c => c.ProductId)
            .WithErrorCode("Validation.Required");
    }

    [Fact]
    public async Task DeleteProduct_WhenProductIsMissing_HasProductNotFoundError() {
        TestValidationResult<DeleteProductCommand> result = await new DeleteProductCommandValidator(new ProductRepositoryStub()).TestValidateAsync(
            new DeleteProductCommand(Guid.NewGuid(), Guid.NewGuid()));

        result.ShouldHaveValidationErrorFor(c => c.ProductId)
            .WithErrorCode("Product.NotFound");
    }

    [Fact]
    public async Task DeleteProduct_WhenProductIsUsed_HasNoValidationErrors() {
        Product product = CreateProduct();
        SetProductUsageCollections(product, mealItemsCount: 1, recipeIngredientsCount: 0);

        TestValidationResult<DeleteProductCommand> result = await new DeleteProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(
            new DeleteProductCommand(product.UserId.Value, product.Id.Value));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task DeleteProduct_WithUnusedProduct_HasNoValidationErrors() {
        Product product = CreateProduct();

        TestValidationResult<DeleteProductCommand> result = await new DeleteProductCommandValidator(new ProductRepositoryStub(product)).TestValidateAsync(
            new DeleteProductCommand(product.UserId.Value, product.Id.Value));

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

        public Task<int> GetUsageCountAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(product?.MealItems.Count + product?.RecipeIngredients.Count ?? 0);

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
