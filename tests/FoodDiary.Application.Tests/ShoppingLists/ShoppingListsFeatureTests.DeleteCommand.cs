using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithEmptyShoppingListId_ReturnsValidationFailure() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));
        Result result = await handler.Handle(new DeleteShoppingListCommand(Guid.NewGuid(), Guid.Empty), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("ShoppingListId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithEmptyUserId_ReturnsInvalidToken() {
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(User.Create("user@example.com", "hash")));

        Result result = await handler.Handle(new DeleteShoppingListCommand(Guid.Empty, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-shopping-delete@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WhenListMissing_ReturnsNotFound() {
        var user = User.Create("shopping-delete-missing@example.com", "hash");
        var handler = new DeleteShoppingListCommandHandler(new NoopShoppingListRepository(), CreateCurrentUserAccessService(user));
        var shoppingListId = Guid.NewGuid();

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, shoppingListId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task DeleteShoppingListCommandHandler_WithList_DeletesList() {
        var user = User.Create("shopping-delete-success@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Weekly");
        var repository = new SingleShoppingListRepository(list);
        var handler = new DeleteShoppingListCommandHandler(repository, CreateCurrentUserAccessService(user));

        Result result = await handler.Handle(new DeleteShoppingListCommand(user.Id.Value, list.Id.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(repository.DeleteCalled);
    }
}
