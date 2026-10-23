using FluentValidation.TestHelper;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Commands.UpdateShoppingList;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Mappings;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.Shopping;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.MealPlanning.Application.Tests.ShoppingLists;

public partial class ShoppingListsFeatureTests {
    [Theory]
    [InlineData(null, "Validation.Required")]
    [InlineData("", "Validation.Required")]
    [InlineData(" ", "Validation.Required")]
    [InlineData("not-a-revision", "Validation.Invalid")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA", "Validation.Invalid")]
    public async Task UpdateShoppingList_RejectsMissingOrMalformedRevisionBeforeMutation(string? revision, string error) {
        var user = User.Create("shopping-revision-validation@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Original");
        list.AddItem("Preserved", productId: null, amount: null, unit: null, category: null, isChecked: false, sortOrder: 0);
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(repository, CreateThrowingProductLookupService(), CreateCurrentUserAccessService(user));
        var command = new UpdateShoppingListCommand(user.Id.Value, list.Id.Value, "Must not apply", [], revision);

        TestValidationResult<UpdateShoppingListCommand> validation = await new UpdateShoppingListCommandValidator().TestValidateAsync(command);
        validation.ShouldHaveValidationErrorFor(value => value.ExpectedRevision).WithErrorCode(error);
        Result<ShoppingListModel> result = await handler.Handle(command, CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal(error, result.Error.Code),
            () => Assert.Equal("Original", list.Name),
            () => Assert.Single(list.Items),
            () => Assert.False(repository.UpdateCalled));
    }

    [Fact]
    public async Task UpdateShoppingList_StaleRevisionRejectsNameAndItemsBeforeProductLookupOrMutation() {
        var user = User.Create("shopping-stale@example.com", "hash");
        var list = ShoppingList.Create(user.Id, "Original");
        string staleRevision = list.ToModel().Revision;
        list.AddItem("Committed by another client", productId: null, amount: null, unit: null, category: null, isChecked: false, sortOrder: 0);
        string currentRevision = list.ToModel().Revision;
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(repository, CreateThrowingProductLookupService(), CreateCurrentUserAccessService(user));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(user.Id.Value, list.Id.Value, "Must not apply", [
                new ShoppingListItemInput(Id: null, Guid.NewGuid(), Name: null, 1, Unit: null, Category: null,
                    Aisle: null, Note: null, IsChecked: false, CheckedOnUtc: null, 0),
            ], staleRevision), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Multiple(
            () => Assert.Equal("ShoppingList.RevisionConflict", result.Error.Code),
            () => Assert.Equal(ErrorKind.Conflict, result.Error.Kind),
            () => Assert.Equal("Original", list.Name),
            () => Assert.Equal(currentRevision, list.ToModel().Revision),
            () => Assert.False(repository.UpdateCalled));
        Assert.Equal("Committed by another client", Assert.Single(list.Items).Name);
    }

    [Fact]
    public async Task UpdateShoppingList_ForeignListWithValidRevisionReturnsNotFound() {
        var owner = User.Create("shopping-revision-owner@example.com", "hash");
        var foreignUser = User.Create("shopping-revision-foreign@example.com", "hash");
        var list = ShoppingList.Create(owner.Id, "Original");
        var repository = new SingleShoppingListRepository(list);
        var handler = new UpdateShoppingListCommandHandler(repository, CreateThrowingProductLookupService(), CreateCurrentUserAccessService(foreignUser));

        Result<ShoppingListModel> result = await handler.Handle(
            new UpdateShoppingListCommand(foreignUser.Id.Value, list.Id.Value, "Must not apply", [], list.ToModel().Revision), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
        Assert.False(repository.UpdateCalled);
        Assert.Equal("Original", list.Name);
    }
}
