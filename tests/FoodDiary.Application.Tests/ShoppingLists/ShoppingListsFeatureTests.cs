using FoodDiary.Application.Products.Common;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.ShoppingLists.Commands.Common;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.ShoppingLists;

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
        Assert.Contains("Unit", result.Error.Message);
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

    private sealed class NoopProductLookupService : IProductLookupService {
        public Task<IReadOnlyDictionary<ProductId, Product>> GetAccessibleByIdsAsync(
            IEnumerable<ProductId> ids,
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<ProductId, Product>>(new Dictionary<ProductId, Product>());
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
