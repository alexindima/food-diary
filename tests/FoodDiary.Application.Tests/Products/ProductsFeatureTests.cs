using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Products.Commands.CreateProduct;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsWithRecent;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Products;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Products;

public class ProductsFeatureTests
{
    [Fact]
    public async Task GetProductsWithRecentQueryValidator_WithEmptyUserId_Fails()
    {
        var validator = new GetProductsWithRecentQueryValidator();
        var query = new GetProductsWithRecentQuery(UserId.Empty, 1, 10, null, true);

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task GetRecentProductsQueryValidator_WithValidUserId_Passes()
    {
        var validator = new GetRecentProductsQueryValidator();
        var query = new GetRecentProductsQuery(UserId.New(), 10, true);

        var result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task GetProductsQueryHandler_WithMissingUserId_ReturnsInvalidToken()
    {
        var handler = new GetProductsQueryHandler(new NoopProductRepository());
        var query = new GetProductsQuery(null, 1, 10, null, true);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task CreateProductCommandHandler_WithMissingUserId_ReturnsInvalidToken()
    {
        var handler = new CreateProductCommandHandler(new NoopProductRepository());
        var command = new CreateProductCommand(
            UserId: null,
            Barcode: null,
            Name: "Apple",
            Brand: null,
            ProductType: "Unknown",
            Category: null,
            Description: null,
            Comment: null,
            ImageUrl: null,
            ImageAssetId: null,
            BaseUnit: "G",
            BaseAmount: 100,
            DefaultPortionAmount: 100,
            CaloriesPerBase: 52,
            ProteinsPerBase: 0.3,
            FatsPerBase: 0.2,
            CarbsPerBase: 14,
            FiberPerBase: 2.4,
            AlcoholPerBase: 0,
            Visibility: "Private");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    private sealed class NoopProductRepository : IProductRepository
    {
        public Task<Product> AddAsync(Product product) => Task.FromResult(product);

        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
            UserId userId,
            bool includePublic,
            int page,
            int limit,
            string? search,
            IReadOnlyCollection<ProductType>? productTypes = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((Items: (IReadOnlyList<(Product Product, int UsageCount)>)Array.Empty<(Product Product, int UsageCount)>(), TotalItems: 0));

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<Product?>(null);

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());

        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>>(new Dictionary<ProductId, (Product Product, int UsageCount)>());

        public Task UpdateAsync(Product product) => Task.CompletedTask;
        public Task DeleteAsync(Product product) => Task.CompletedTask;
    }
}
