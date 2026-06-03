using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public class ShoppingListsFeatureTests {
    [Fact]
    public async Task GetCurrentShoppingListQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetCurrentShoppingListQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var result = await handler.Handle(new GetCurrentShoppingListQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetShoppingListByIdQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var result = await handler.Handle(new GetShoppingListByIdQuery(null, Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetShoppingListByIdQueryHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new GetShoppingListByIdQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var result = await handler.Handle(new GetShoppingListByIdQuery(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new GetShoppingListsQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var result = await handler.Handle(new GetShoppingListsQuery(null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), new StubUserRepository(User.Create("user@example.com", "hash")));
        var result = await handler.Handle(new DeleteShoppingListCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));
        var result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.Empty, "Weekly", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithMissingUserId_ReturnsInvalidToken() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new UpdateShoppingListCommand(null, Guid.NewGuid(), "Weekly", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithNothingToUpdate_ReturnsRequiredItems() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Required", result.Error.Code);
        Assert.Contains("Items", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WhenListIsMissing_ReturnsNotFound() {
        var handler = new UpdateShoppingListCommandHandler(
            new NoopShoppingListRepository(),
            new NoopProductLookupService(),
            new StubUserRepository(User.Create("user@example.com", "hash")));

        var shoppingListId = Guid.NewGuid();
        var result = await handler.Handle(
            new UpdateShoppingListCommand(Guid.NewGuid(), shoppingListId, "Weekly", null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
        Assert.Contains(shoppingListId.ToString(), result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithInaccessibleProduct_ReturnsProductNotAccessible() {
        var user = User.Create("shopping-owner@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Old");
        var handler = new UpdateShoppingListCommandHandler(
            new SingleShoppingListRepository(list),
            new NoopProductLookupService(),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                null,
                [new ShoppingListItemInput(ProductId.New().Value, null, 1, null, null, false, null)]),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product.NotAccessible", result.Error.Code);
    }

    [Fact]
    public async Task UpdateShoppingListCommandHandler_WithNameAndItems_ReplacesItemsAndReturnsModel() {
        var user = User.Create("shopping-update@example.com", "hash");
        var product = Product.Create(
            user.Id,
            "Milk",
            MeasurementUnit.Ml,
            100,
            250,
            60,
            3,
            2,
            5,
            0,
            0,
            category: "Dairy");
        var list = ShoppingList.Create(user.Id, "Old");
        list.AddItem("Old item", null, 1, null, null, false, 1);
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(
            repository,
            new ProductLookupService(product),
            new StubUserRepository(user));

        var result = await handler.Handle(
            new UpdateShoppingListCommand(
                user.Id.Value,
                list.Id.Value,
                "  Weekly groceries  ",
                [
                    new ShoppingListItemInput(product.Id.Value, null, 2, null, null, true, null),
                    new ShoppingListItemInput(null, "  Apples  ", 3, "Pcs", "Fruit", false, 7)
                ]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(repository.UpdateCalled);
        Assert.Equal("Weekly groceries", result.Value.Name);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Contains(result.Value.Items, item =>
            item.ProductId == product.Id.Value &&
            string.Equals(item.Name, "Milk", StringComparison.Ordinal) &&
            string.Equals(item.Unit, "Ml", StringComparison.Ordinal) &&
            string.Equals(item.Category, "Dairy", StringComparison.Ordinal) &&
            item.SortOrder == 1);
        Assert.Contains(result.Value.Items, item =>
            item.ProductId is null &&
            string.Equals(item.Name, "Apples", StringComparison.Ordinal) &&
            string.Equals(item.Unit, "Pcs", StringComparison.Ordinal) &&
            string.Equals(item.Category, "Fruit", StringComparison.Ordinal) &&
            item.SortOrder == 7);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithInvalidUnit_FailsWithUnitField() {
        var items = new[] {
            new ShoppingListItemInput(null, "Milk", 1, "invalid_unit", null, false, 1)
        };

        var result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains("Unit", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithNonPositiveAmount_Fails() {
        var items = new[] {
            new ShoppingListItemInput(null, "Milk", 0, null, null, false, 1)
        };

        var result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public async Task ShoppingListItemBuilder_WithNonFiniteAmount_Fails(double amount) {
        var items = new[] {
            new ShoppingListItemInput(null, "Milk", amount, null, null, false, 1)
        };

        var result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("finite", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ShoppingListItemBuilder_WithEmptyProductId_FailsWithValidationError() {
        var items = new[] {
            new ShoppingListItemInput(Guid.Empty, null, 1, null, null, false, 1)
        };

        var result = await ShoppingListItemBuilder.BuildItemsAsync(
            items,
            UserId.New(),
            new NoopProductLookupService(),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ProductId", result.Error.Message, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopShoppingListRepository : IShoppingListRepository {
        public Task<ShoppingList> AddAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.FromResult(list);

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) => Task.FromResult<ShoppingList?>(null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ShoppingList>>([]);

        public Task UpdateAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(ShoppingList list, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SingleShoppingListRepository(ShoppingList list) : IShoppingListRepository {
        public bool UpdateCalled { get; private set; }

        public Task<ShoppingList> AddAsync(ShoppingList addedList, CancellationToken cancellationToken = default) =>
            Task.FromResult(addedList);

        public Task<ShoppingList?> GetByIdAsync(
            ShoppingListId id,
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingList?>(id == list.Id && userId == list.UserId ? list : null);

        public Task<ShoppingList?> GetCurrentAsync(
            UserId userId,
            bool includeItems = false,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ShoppingList?>(userId == list.UserId ? list : null);

        public Task<IReadOnlyList<ShoppingList>> GetAllAsync(
            UserId userId,
            bool includeItems = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ShoppingList>>(userId == list.UserId ? [list] : []);

        public Task UpdateAsync(ShoppingList updatedList, CancellationToken cancellationToken = default) {
            UpdateCalled = true;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ShoppingList deletedList, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class ProductLookupService(params Product[] products) : IProductLookupService {
        private readonly Dictionary<ProductId, Product> _products = products.ToDictionary(product => product.Id);

        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(
                ids.Where(_products.ContainsKey).ToDictionary(id => id, id => _products[id]));
    }

    [Fact]
    public async Task GetShoppingListsQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new GetShoppingListsQueryHandler(new NoopShoppingListRepository(), new StubUserRepository(user));

        var result = await handler.Handle(new GetShoppingListsQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User user) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user);
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User addedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User updatedUser, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
