using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Usda.Commands.LinkProductToUsdaFood;
using FoodDiary.Application.Usda.Commands.UnlinkProductFromUsdaFood;
using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Usda;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Usda;

public class UsdaFeatureTests {
    [Fact]
    public async Task LinkProductToUsdaFood_WithValidData_Succeeds() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, null, 165, 31, 3.6, 0, 0, 0);
        var usdaFood = new UsdaFood { FdcId = 171077, Description = "Chicken, breast" };
        var productRepo = new StubProductRepository(product);
        var usdaRepo = new StubUsdaFoodRepository(usdaFood);

        var handler = new LinkProductToUsdaFoodCommandHandler(productRepo, usdaRepo);
        var result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(userId.Value, product.Id.Value, 171077),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(productRepo.UpdateCalled);
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WhenProductNotFound_ReturnsFailure() {
        var handler = new LinkProductToUsdaFoodCommandHandler(
            new StubProductRepository(null), new StubUsdaFoodRepository(null));

        var result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(Guid.NewGuid(), Guid.NewGuid(), 171077),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WhenUsdaFoodNotFound_ReturnsFailure() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, null, 165, 31, 3.6, 0, 0, 0);
        var handler = new LinkProductToUsdaFoodCommandHandler(
            new StubProductRepository(product), new StubUsdaFoodRepository(null));

        var result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(userId.Value, product.Id.Value, 999999),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("FoodNotFound", result.Error.Code);
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WithValidData_Succeeds() {
        var userId = UserId.New();
        var product = Product.Create(userId, "Chicken", MeasurementUnit.G, 100, null, 165, 31, 3.6, 0, 0, 0);
        var productRepo = new StubProductRepository(product);

        var handler = new UnlinkProductFromUsdaFoodCommandHandler(productRepo);
        var result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(userId.Value, product.Id.Value),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(productRepo.UpdateCalled);
    }

    [Fact]
    public async Task UnlinkProductFromUsdaFood_WhenProductNotFound_ReturnsFailure() {
        var handler = new UnlinkProductFromUsdaFoodCommandHandler(new StubProductRepository(null));

        var result = await handler.Handle(
            new UnlinkProductFromUsdaFoodCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task LinkProductToUsdaFood_WithNullUserId_ReturnsFailure() {
        var handler = new LinkProductToUsdaFoodCommandHandler(
            new StubProductRepository(null), new StubUsdaFoodRepository(null));

        var result = await handler.Handle(
            new LinkProductToUsdaFoodCommand(null, Guid.NewGuid(), 1), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    private sealed class StubProductRepository(Product? product) : IProductRepository {
        public bool UpdateCalled { get; private set; }

        public Task<Product?> GetByIdAsync(ProductId id, UserId userId, bool includePublic = true, CancellationToken ct = default) =>
            Task.FromResult(product);

        public Task UpdateAsync(Product p, CancellationToken ct = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task<Product> AddAsync(Product p, CancellationToken ct = default) => throw new NotSupportedException();
        public Task DeleteAsync(Product p, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<(Product Product, int UsageCount)> Items, int TotalItems)> GetPagedAsync(UserId userId, bool includePublic, int page, int limit, string? search, IReadOnlyCollection<ProductType>? productTypes = null, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(IEnumerable<ProductId> ids, UserId userId, bool includePublic = true, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<ProductId, (Product Product, int UsageCount)>> GetByIdsWithUsageAsync(IEnumerable<ProductId> ids, UserId userId, bool includePublic = true, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class StubUsdaFoodRepository(UsdaFood? food) : IUsdaFoodRepository {
        public Task<UsdaFood?> GetByFdcIdAsync(int fdcId, CancellationToken ct = default) =>
            Task.FromResult(food);

        public Task<IReadOnlyList<UsdaFood>> SearchAsync(string query, int limit = 20, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(int fdcId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(int fdcId, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(IEnumerable<int> fdcIds, CancellationToken ct = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(string ageGroup = "adult", string gender = "all", CancellationToken ct = default) => throw new NotSupportedException();
    }
}
