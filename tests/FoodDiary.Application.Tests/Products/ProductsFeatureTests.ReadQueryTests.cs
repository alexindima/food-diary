using FoodDiary.Results;
using FoodDiary.Application.Products.Queries.GetProductById;
using FoodDiary.Application.Products.Queries.GetProducts;
using FoodDiary.Application.Products.Queries.GetProductsOverview;
using FoodDiary.Application.Products.Queries.GetRecentProducts;
using FoodDiary.Application.Abstractions.RecentItems.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Products.Models;
using FluentValidation.Results;
using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Tests.Products;

public partial class ProductsFeatureTests {

    [Fact]
    public async Task GetProductsOverviewQueryValidator_WithEmptyUserId_Fails() {
        var validator = new GetProductsOverviewQueryValidator();
        var query = new GetProductsOverviewQuery(Guid.Empty, 1, 10, Search: null, IncludePublic: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
    }


    [Fact]
    public async Task GetRecentProductsQueryValidator_WithValidUserId_Passes() {
        var validator = new GetRecentProductsQueryValidator();
        var query = new GetRecentProductsQuery(Guid.NewGuid(), 10, IncludePublic: true);

        ValidationResult result = await validator.ValidateAsync(query);

        Assert.True(result.IsValid);
    }


    [Fact]
    public async Task GetProductsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetProductsQueryHandler(new OverviewProductReadService(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var query = new GetProductsQuery(UserId: null, 1, 10, Search: null, IncludePublic: true);

        Result<PagedResponse<ProductModel>> result = await handler.Handle(query, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetProductsQueryHandler_ReturnsPagedProductsAndAppliesProductTypeFilter() {
        var user = User.Create("products-page@example.com", "hash");
        var owned = Product.Create(
            user.Id,
            name: "Owned Apple",
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
        var supplement = Product.Create(
            user.Id,
            name: "Protein",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 30,
            caloriesPerBase: 120,
            proteinsPerBase: 24,
            fatsPerBase: 2,
            carbsPerBase: 4,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            productType: ProductType.Dairy,
            visibility: Visibility.Private);
        var repository = new OverviewProductReadService([(owned, 4), (supplement, 2)]);
        var handler = new GetProductsQueryHandler(repository, new StubUserRepository(user));

        Result<PagedResponse<ProductModel>> result = await handler.Handle(
            new GetProductsQuery(user.Id.Value, Page: 0, Limit: 0, Search: "ignored", IncludePublic: true, ProductTypes: ["fruit", "Fruit", "invalid"]),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(1, result.Value.Limit);
        ProductModel item = Assert.Single(result.Value.Data);
        Assert.Equal(owned.Id.Value, item.Id);
        Assert.Equal(4, item.UsageCount);
        Assert.True(item.IsOwnedByCurrentUser);
    }


    [Fact]
    public async Task GetProductByIdQueryHandler_WithEmptyProductId_ReturnsValidationFailure() {
        var handler = new GetProductByIdQueryHandler(new OverviewProductReadService(), new StubUserRepository(User.Create("user@example.com", "hash")));

        Result<ProductModel> result = await handler.Handle(
            new GetProductByIdQuery(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public async Task GetProductByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetProductByIdQueryHandler(
            new OverviewProductReadService(),
            new StubUserRepository(User.Create("product-by-id-invalid-token@example.com", "hash")));

        Result<ProductModel> result = await handler.Handle(
            new GetProductByIdQuery(UserId: null, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetProductByIdQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("product-by-id-deleted-user@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetProductByIdQueryHandler(
            new OverviewProductReadService(),
            new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(
            new GetProductByIdQuery(user.Id.Value, ProductId.New().Value),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task GetProductsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-product@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetProductsQueryHandler(new OverviewProductReadService(), new StubUserRepository(user));

        Result<PagedResponse<ProductModel>> result = await handler.Handle(
            new GetProductsQuery(user.Id.Value, 1, 10, Search: null, IncludePublic: true),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }


    [Fact]
    public async Task GetProductByIdQueryHandler_WithAccessibleProduct_ReturnsUsageAndOwnerComment() {
        var user = User.Create("product-by-id@example.com", "hash");
        var product = Product.Create(
            user.Id,
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
            comment: "Private note",
            visibility: Visibility.Private);

        SetProductUsageCollections(product, mealItemsCount: 2, recipeIngredientsCount: 1);
        var handler = new GetProductByIdQueryHandler(
            new OverviewProductReadService(productsByIdWithUsage: new Dictionary<ProductId, (Product Product, int UsageCount)> {
                [product.Id] = (product, 3),
            }),
            new StubUserRepository(user));

        Result<ProductModel> result = await handler.Handle(new GetProductByIdQuery(user.Id.Value, product.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(product.Id.Value, result.Value.Id);
        Assert.Equal(3, result.Value.UsageCount);
        Assert.True(result.Value.IsOwnedByCurrentUser);
        Assert.Equal("Private note", result.Value.Comment);
    }


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


    [Fact]
    public async Task GetRecentProductsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        GetRecentProductsQueryHandler handler = CreateRecentProductsHandler(new StubRecentItemRepository([]), new OverviewProductReadService());

        Result<IReadOnlyList<ProductModel>> result = await handler.Handle(new GetRecentProductsQuery(UserId: null, 10, IncludePublic: true), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }


    [Fact]
    public async Task GetRecentProductsQueryHandler_WhenNoRecentProducts_ReturnsEmptyList() {
        var userId = UserId.New();
        var recentRepository = new StubRecentItemRepository([]);
        GetRecentProductsQueryHandler handler = CreateRecentProductsHandler(recentRepository, new OverviewProductReadService());

        Result<IReadOnlyList<ProductModel>> result = await handler.Handle(new GetRecentProductsQuery(userId.Value, 10, IncludePublic: true), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(result.Value);
        Assert.Equal(1, recentRepository.GetRecentProductsCallCount);
    }


    [Fact]
    public async Task GetRecentProductsQueryHandler_ReturnsProductsInRecentOrderAndSkipsMissingItems() {
        var userId = UserId.New();
        var owned = Product.Create(
            userId,
            name: "Owned Apple",
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
        var publicProduct = Product.Create(
            UserId.New(),
            name: "Public Yogurt",
            baseUnit: MeasurementUnit.G,
            baseAmount: 100,
            defaultPortionAmount: 150,
            caloriesPerBase: 73,
            proteinsPerBase: 9.5,
            fatsPerBase: 2.1,
            carbsPerBase: 3.8,
            fiberPerBase: 0,
            alcoholPerBase: 0,
            visibility: Visibility.Public);
        var missingProductId = ProductId.New();
        var readService = new OverviewProductReadService(
            productsByIdWithUsage: new Dictionary<ProductId, (Product Product, int UsageCount)> {
                [owned.Id] = (owned, 7),
                [publicProduct.Id] = (publicProduct, 3),
            });
        var recentRepository = new StubRecentItemRepository([
            new RecentProductUsage(publicProduct.Id, 3, DateTime.UtcNow),
            new RecentProductUsage(missingProductId, 9, DateTime.UtcNow),
            new RecentProductUsage(owned.Id, 7, DateTime.UtcNow),
        ]);
        GetRecentProductsQueryHandler handler = CreateRecentProductsHandler(recentRepository, readService);

        Result<IReadOnlyList<ProductModel>> result = await handler.Handle(new GetRecentProductsQuery(userId.Value, 99, IncludePublic: true), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal([publicProduct.Id.Value, owned.Id.Value], [.. result.Value.Select(x => x.Id)]);
        Assert.False(result.Value[0].IsOwnedByCurrentUser);
        Assert.True(result.Value[1].IsOwnedByCurrentUser);
        Assert.Equal(3, result.Value[0].UsageCount);
        Assert.Equal(7, result.Value[1].UsageCount);
    }

}
