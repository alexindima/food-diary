using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProductRepositoryDefaultMethodTests {
    [Fact]
    public async Task ProductRepository_GetByIdForUpdateAsync_DelegatesToGetByIdAsync() {
        var stub = new RecordingProductRepository();
        IProductRepository repository = stub;
        var productId = ProductId.New();
        var userId = UserId.New();
        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.GetByIdForUpdateAsync(productId, userId, includePublic: false, cancellationTokenSource.Token);

        Assert.Equal(productId, stub.CapturedProductId);
        Assert.Equal(userId, stub.CapturedUserId);
        Assert.False(stub.CapturedIncludePublic);
        Assert.Equal(cancellationTokenSource.Token, stub.CapturedCancellationToken);
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingProductRepository : IProductRepository {
        public ProductId CapturedProductId { get; private set; }
        public UserId CapturedUserId { get; private set; }
        public bool CapturedIncludePublic { get; private set; }
        public CancellationToken CapturedCancellationToken { get; private set; }

        public Task<Product> AddAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Product?> GetByIdAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) {
            CapturedProductId = id;
            CapturedUserId = userId;
            CapturedIncludePublic = includePublic;
            CapturedCancellationToken = cancellationToken;
            return Task.FromResult<Product?>(null);
        }

        public Task<IReadOnlyDictionary<ProductId, Product>> GetByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<int> GetUsageCountAsync(
            ProductId id,
            UserId userId,
            bool includePublic = true,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task DeleteAsync(Product product, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
