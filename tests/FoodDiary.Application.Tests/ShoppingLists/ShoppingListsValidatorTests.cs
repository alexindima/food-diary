using FluentValidation.TestHelper;
using FoodDiary.Application.MealPlanning.ShoppingLists.Commands.CreateShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.MealPlanning.ShoppingLists.Queries.GetShoppingLists;

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
    public async Task CreateShoppingList_WithNameOverDomainLimit_HasError() {
        TestValidationResult<CreateShoppingListCommand> result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(Guid.NewGuid(), new string('x', 129), []));

        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public async Task CreateShoppingList_WithTooManyItems_HasError() {
        ShoppingListItemInput[] items = [.. Enumerable.Range(0, 501).Select(_ => CreateValidItem())];

        TestValidationResult<CreateShoppingListCommand> result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(Guid.NewGuid(), "List", items));

        result.ShouldHaveValidationErrorFor(c => c.Items);
    }

    [Fact]
    public async Task CreateShoppingList_WithItemFieldsOutsideDomainLimits_HasErrors() {
        ShoppingListItemInput[] items = [
            CreateValidItem() with { Name = new string('x', 257) },
            CreateValidItem() with { Category = new string('x', 129) },
            CreateValidItem() with { Aisle = new string('x', 129) },
            CreateValidItem() with { Note = new string('x', 513) },
            CreateValidItem() with { Amount = 1_000_001d },
        ];

        TestValidationResult<CreateShoppingListCommand> result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(Guid.NewGuid(), "List", items));

        Assert.Equal(5, result.Errors.Count);
        Assert.All(result.Errors, static error => Assert.Equal("Validation.Invalid", error.ErrorCode));
    }

    [Fact]
    public async Task CreateShoppingList_AtDomainAndCollectionLimits_HasNoErrors() {
        ShoppingListItemInput boundaryItem = CreateValidItem() with {
            Name = new string('x', 256),
            Category = new string('x', 128),
            Aisle = new string('x', 128),
            Note = new string('x', 512),
            Amount = 1_000_000d,
        };
        ShoppingListItemInput[] items = [.. Enumerable.Repeat(boundaryItem, 500)];

        TestValidationResult<CreateShoppingListCommand> result = await new CreateShoppingListCommandValidator().TestValidateAsync(
            new CreateShoppingListCommand(Guid.NewGuid(), new string('x', 128), items));

        result.ShouldNotHaveAnyValidationErrors();
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
    public async Task UpdateShoppingList_WithNameOverDomainLimit_HasError() {
        TestValidationResult<UpdateShoppingListCommand> result = await new UpdateShoppingListCommandValidator().TestValidateAsync(
            new UpdateShoppingListCommand(Guid.NewGuid(), Guid.NewGuid(), new string('x', 129), Items: null));

        result.ShouldHaveValidationErrorFor(c => c.Name);
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

    private static ShoppingListItemInput CreateValidItem() =>
        new(
            Id: null,
            ProductId: null,
            Name: "Milk",
            Amount: 1,
            Unit: null,
            Category: null,
            Aisle: null,
            Note: null,
            IsChecked: false,
            CheckedOnUtc: null,
            SortOrder: null);
}
