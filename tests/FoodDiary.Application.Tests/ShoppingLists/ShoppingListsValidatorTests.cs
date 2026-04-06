using FluentValidation.TestHelper;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

namespace FoodDiary.Application.Tests.ShoppingLists;

public class ShoppingListsValidatorTests {
    [Fact]
    public async Task CreateShoppingList_WithNullUserId_HasError() {
        var result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(null, "List", []));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateShoppingList_WithEmptyName_HasError() {
        var result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(Guid.NewGuid(), "", []));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task DeleteShoppingList_WithEmptyId_HasError() {
        var result = await new DeleteShoppingListCommandValidator().TestValidateAsync(
            new DeleteShoppingListCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ShoppingListId);
    }

    [Fact]
    public async Task UpdateShoppingList_WithEmptyId_HasError() {
        var result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.Empty, null, null));
        result.ShouldHaveValidationErrorFor(c => c.ShoppingListId);
    }

    [Fact]
    public async Task UpdateShoppingList_WithNothingToUpdate_HasError() {
        var result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), null, null));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpdateShoppingList_WithName_NoErrors() {
        var result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated", null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GetCurrentShoppingList_WithNullUserId_HasError() {
        var result = await new GetCurrentShoppingListQueryValidator().TestValidateAsync(
            new GetCurrentShoppingListQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetShoppingListById_WithEmptyId_HasError() {
        var result = await new GetShoppingListByIdQueryValidator().TestValidateAsync(
            new GetShoppingListByIdQuery(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ShoppingListId);
    }

    [Fact]
    public async Task GetShoppingLists_WithNullUserId_HasError() {
        var result = await new GetShoppingListsQueryValidator().TestValidateAsync(
            new GetShoppingListsQuery(null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
