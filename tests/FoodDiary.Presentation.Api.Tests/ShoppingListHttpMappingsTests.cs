using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class ShoppingListHttpMappingsTests {
    [Fact]
    public void ToCurrentQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToCurrentQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToListQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToListQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void ToGetByIdQuery_MapsIds() {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        var query = listId.ToGetByIdQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(listId, query.ShoppingListId);
    }

    [Fact]
    public void ToDeleteCommand_MapsIds() {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        var command = listId.ToDeleteCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(listId, command.ShoppingListId);
    }

    [Fact]
    public void CreateRequest_ToCommand_MapsNameAndItems() {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var request = new CreateShoppingListHttpRequest("Groceries", new List<ShoppingListItemHttpRequest> {
            new(productId, "Milk", 2.0, "L", "Dairy", false, 1),
        });

        var command = request.ToCommand(userId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal("Groceries", command.Name);
        Assert.Single(command.Items);
        Assert.Equal(productId, command.Items[0].ProductId);
        Assert.Equal("Milk", command.Items[0].Name);
        Assert.Equal(2.0, command.Items[0].Amount);
    }

    [Fact]
    public void CreateRequest_ToCommand_WithNullItems_CreatesEmptyList() {
        var userId = Guid.NewGuid();
        var request = new CreateShoppingListHttpRequest("Empty List");

        var command = request.ToCommand(userId);

        Assert.Empty(command.Items);
    }

    [Fact]
    public void UpdateRequest_ToCommand_MapsAllFields() {
        var userId = Guid.NewGuid();
        var listId = Guid.NewGuid();
        var request = new UpdateShoppingListHttpRequest("Updated Name", new List<ShoppingListItemHttpRequest> {
            new(null, "Eggs", 12, "pcs", "Protein", true, 2),
        });

        var command = request.ToCommand(userId, listId);

        Assert.Equal(userId, command.UserId);
        Assert.Equal(listId, command.ShoppingListId);
        Assert.Equal("Updated Name", command.Name);
        Assert.NotNull(command.Items);
        Assert.Single(command.Items!);
    }

    [Fact]
    public void ShoppingListSummaryModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var model = new ShoppingListSummaryModel(id, "Weekly", createdAt, 5);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("Weekly", response.Name);
        Assert.Equal(createdAt, response.CreatedAt);
        Assert.Equal(5, response.ItemsCount);
    }

    [Fact]
    public void ShoppingListModel_ToHttpResponse_MapsItemsCorrectly() {
        var listId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;
        var items = new List<ShoppingListItemModel> {
            new(itemId, listId, productId, "Bread", 1, "loaf", "Bakery", false, 0),
        };
        var model = new ShoppingListModel(listId, "Shopping", createdAt, items);

        var response = model.ToHttpResponse();

        Assert.Equal(listId, response.Id);
        Assert.Equal("Shopping", response.Name);
        Assert.Single(response.Items);
        Assert.Equal(itemId, response.Items[0].Id);
        Assert.Equal(productId, response.Items[0].ProductId);
        Assert.Equal("Bread", response.Items[0].Name);
    }
}
