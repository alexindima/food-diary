using FoodDiary.Results;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;

namespace FoodDiary.Application.Tests.Products;

public partial class ProductsFeatureTests {

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithoutSearch_ReturnsRecentFavoritesAndFavoriteFlags() {
        var user = User.Create("overview-products@example.com", "hash");
        var breakfast = Product.Create(
            user.Id,
            name: "Breakfast Yogurt",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 73,
            proteinsPerBase: 9.5,
            fatsPerBase: 2.1,
            carbsPerBase: 3.8,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var lunch = Product.Create(
            user.Id,
            name: "Lunch Chicken",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 165,
            proteinsPerBase: 31,
            fatsPerBase: 3.6,
            carbsPerBase: 0,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

        var favorite = FavoriteProduct.Create(user.Id, lunch.Id, "Fav lunch");
        SetFavoriteProductNavigation(favorite, lunch);

        var overviewReadService = new OverviewProductReadService(
            pagedItems: [(breakfast, 2), (lunch, 5)],
            productsByIdWithUsage: new Dictionary<ProductId, (Product Product, int UsageCount)> {
                [lunch.Id] = (lunch, 5),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentProductUsage(lunch.Id, 5, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteProductRepository([favorite]);
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(overviewReadService, recentRepository, favoriteRepository, new StubUserRepository(user));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.AllProducts.Data.Count);
        Assert.Single(result.Value.RecentItems);
        Assert.Single(result.Value.FavoriteItems);
        Assert.Equal(1, result.Value.FavoriteTotalCount);
        Assert.Equal(lunch.Id.Value, result.Value.RecentItems[0].Id);
        Assert.True(result.Value.RecentItems[0].IsFavorite);
        Assert.Equal(favorite.Id.Value, result.Value.RecentItems[0].FavoriteProductId);
        Assert.True(result.Value.AllProducts.Data.Single(x => x.Id == lunch.Id.Value).IsFavorite);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithSearch_SkipsRecentItems() {
        var user = User.Create("overview-search@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Protein Bar",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 60,
            caloriesPerBase: 380,
            proteinsPerBase: 30,
            fatsPerBase: 10,
            carbsPerBase: 40,
            fiberPerBase: 5,
            alcoholPerBase: 0,
            visibility: Visibility.Private);

        var overviewReadService = new OverviewProductReadService(pagedItems: [(product, 1)]);
        var recentRepository = new StubRecentItemRepository([
            new RecentProductUsage(product.Id, 1, DateTime.UtcNow),
        ]);
        var favoriteRepository = new StubFavoriteProductRepository([]);
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(overviewReadService, recentRepository, favoriteRepository, new StubUserRepository(user));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, "protein", IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(0, recentRepository.GetRecentProductsCallCount);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WhenUserAccessFails_ReturnsAccessFailure() {
        var user = User.Create("overview-inactive-product-user@example.com", "hash");
        user.Deactivate();
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(
            new OverviewProductReadService(),
            new StubRecentItemRepository([]),
            new StubFavoriteProductRepository([]),
            new StubUserRepository(user));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(
            new OverviewProductReadService(),
            new StubRecentItemRepository([]),
            new StubFavoriteProductRepository([]),
            new StubUserRepository(User.Create("overview-missing-user@example.com", "hash")));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(UserId: null, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WhenNoRecentProducts_ReturnsEmptyRecentItems() {
        var user = User.Create("overview-empty-recents@example.com", "hash");
        var product = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            visibility: Visibility.Private);
        var recentRepository = new StubRecentItemRepository([]);
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(
            new OverviewProductReadService(pagedItems: [(product, 1)]),
            recentRepository,
            new StubFavoriteProductRepository([]),
            new StubUserRepository(user));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, 10, 10),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value.RecentItems);
        Assert.Equal(1, recentRepository.GetRecentProductsCallCount);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithHasImageFilter_FiltersRecentItems() {
        var user = User.Create("overview-product-image-filter@example.com", "hash");
        Product withImage = CreateProduct(user.Id, "Photo Yogurt", imageUrl: "https://cdn.test/yogurt.jpg");
        Product withoutImage = CreateProduct(user.Id, "Plain Yogurt");
        var overviewReadService = new OverviewProductReadService(
            productsByIdWithUsage: new Dictionary<ProductId, (Product Product, int UsageCount)> {
                [withImage.Id] = (withImage, 4),
                [withoutImage.Id] = (withoutImage, 2),
            });
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(
            overviewReadService,
            new StubRecentItemRepository([
                new RecentProductUsage(withImage.Id, 4, DateTime.UtcNow),
                new RecentProductUsage(withoutImage.Id, 2, DateTime.UtcNow),
            ]),
            new StubFavoriteProductRepository([]),
            new StubUserRepository(user));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true, HasImage: true),
            CancellationToken.None);

        ResultAssert.Success(result);
        ProductModel recent = Assert.Single(result.Value.RecentItems);
        Assert.Equal(withImage.Id.Value, recent.Id);
    }

    [Fact]
    public async Task GetProductsOverviewQueryHandler_WithProductTypes_FiltersDistinctValidTypes() {
        var user = User.Create("overview-product-types@example.com", "hash");
        var dairy = Product.Create(
            user.Id,
            name: "Greek Yogurt",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 73,
            proteinsPerBase: 9.5,
            fatsPerBase: 2.1,
            carbsPerBase: 3.8,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            productType: ProductType.Dairy,
            visibility: Visibility.Private);
        var fruit = Product.Create(
            user.Id,
            name: "Apple",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 120,
            caloriesPerBase: 52,
            proteinsPerBase: 0.3,
            fatsPerBase: 0.2,
            carbsPerBase: 14,
            fiberPerBase: 2.4,
            alcoholPerBase: 0,
            productType: ProductType.Fruit,
            visibility: Visibility.Private);
        GetProductsOverviewQueryHandler handler = CreateProductsOverviewHandler(
            new OverviewProductReadService(pagedItems: [(dairy, 3), (fruit, 2)]),
            new StubRecentItemRepository([]),
            new StubFavoriteProductRepository([]),
            new StubUserRepository(user));

        Result<ProductOverviewModel> result = await handler.Handle(
            new GetProductsOverviewQuery(
                user.Id.Value,
                Page: 1,
                Limit: 10,
                Search: null,
                IncludePublic: true,
                ProductTypes: ["dairy", "Dairy", "not-a-type"]),
            CancellationToken.None);

        ResultAssert.Success(result);
        ProductModel item = Assert.Single(result.Value.AllProducts.Data);
        Assert.Equal(dairy.Id.Value, item.Id);
        Assert.Equal(ProductType.Dairy.ToString(), item.ProductType);
    }

}
