using FluentValidation.TestHelper;
using FoodDiary.Application.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;

namespace FoodDiary.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public class ShoppingListsValidatorTests {
    [Fact]
    public async Task CreateShoppingList_WithNullUserId_HasError() {
        TestValidationResult<CreateShoppingListCommand> result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(UserId: null, "List", []));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task CreateShoppingList_WithEmptyName_HasError() {
        TestValidationResult<CreateShoppingListCommand> result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(Guid.NewGuid(), "", []));
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task DeleteShoppingList_WithEmptyId_HasError() {
        TestValidationResult<DeleteShoppingListCommand> result = await new DeleteShoppingListCommandValidator().TestValidateAsync(
            new DeleteShoppingListCommand(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ShoppingListId);
    }

    [Fact]
    public async Task UpdateShoppingList_WithEmptyId_HasError() {
        TestValidationResult<UpdateShoppingListCommand> result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.Empty, Name: null, Items: null));
        result.ShouldHaveValidationErrorFor(c => c.ShoppingListId);
    }

    [Fact]
    public async Task UpdateShoppingList_WithNothingToUpdate_HasError() {
        TestValidationResult<UpdateShoppingListCommand> result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), Name: null, Items: null));
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public async Task UpdateShoppingList_WithName_NoErrors() {
        TestValidationResult<UpdateShoppingListCommand> result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), "Updated", Items: null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task GetCurrentShoppingList_WithNullUserId_HasError() {
        TestValidationResult<GetCurrentShoppingListQuery> result = await new GetCurrentShoppingListQueryValidator().TestValidateAsync(
            new GetCurrentShoppingListQuery(UserId: null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }

    [Fact]
    public async Task GetShoppingListById_WithEmptyId_HasError() {
        TestValidationResult<GetShoppingListByIdQuery> result = await new GetShoppingListByIdQueryValidator().TestValidateAsync(
            new GetShoppingListByIdQuery(Guid.NewGuid(), Guid.Empty));
        result.ShouldHaveValidationErrorFor(c => c.ShoppingListId);
    }

    [Fact]
    public async Task GetShoppingLists_WithNullUserId_HasError() {
        TestValidationResult<GetShoppingListsQuery> result = await new GetShoppingListsQueryValidator().TestValidateAsync(
            new GetShoppingListsQuery(UserId: null));
        result.ShouldHaveValidationErrorFor(c => c.UserId);
    }
}
