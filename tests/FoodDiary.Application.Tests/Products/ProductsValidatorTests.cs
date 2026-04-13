using FluentValidation.TestHelper;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Commands.DuplicateProduct;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Queries.GetRecentProducts;

namespace FoodDiary.Application.Tests.Products;

public class ProductsValidatorTests {
    private static CreateProductCommand ValidCreateProduct(Guid? userId = null) =>
        new(userId ?? Guid.NewGuid(), null, "Chicken", null, "Food", null, null, null, null, null,
            "g", 100, 100, 165, 31, 3.6, 0, 0, 0, "Private");

    // ── CreateProduct ──

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

    // ── DuplicateProduct ──

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

    // ── GetProductById ──

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

    // ── GetProducts ──

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

    // ── GetProductsWithRecent ──

    [Fact]
    public async Task GetProductsOverview_WithNullUserId_HasError() {
        var result = await new GetProductsOverviewQueryValidator().TestValidateAsync(
            new GetProductsOverviewQuery(null, 1, 10, null, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    // ── GetRecentProducts ──

    [Fact]
    public async Task GetRecentProducts_WithNullUserId_HasError() {
        var result = await new GetRecentProductsQueryValidator().TestValidateAsync(
            new GetRecentProductsQuery(null, 10, false));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
