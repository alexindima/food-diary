using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Application.ShoppingLists.Services;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.ShoppingLists;

[ExcludeFromCodeCoverage]
public sealed class ShoppingListCreationServiceTests {
    [Fact]
    public async Task CreateAsync_OrdersItemsAddsSourcesAndPersistsAggregate() {
        IShoppingListWriteRepository repository = Substitute.For<IShoppingListWriteRepository>();
        ShoppingList? persisted = null;
        repository
            .AddAsync(Arg.Do<ShoppingList>(list => persisted = list), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ShoppingList>());
        var service = new ShoppingListCreationService(repository);
        var mealPlanId = MealPlanId.New();
        var mealPlanMealId = MealPlanMealId.New();
        var recipeId = RecipeId.New();
        var request = new ShoppingListCreationRequest(
            UserId.New(),
            "Weekly groceries",
            [
                new ShoppingListCreationItem(
                    ProductId.New(), "Second", 2, MeasurementUnit.Pcs, "Other", 2, []),
                new ShoppingListCreationItem(
                    ProductId.New(), "First", 150, MeasurementUnit.G, "Protein", 1,
                    [new ShoppingListCreationSource(
                        mealPlanId,
                        mealPlanMealId,
                        recipeId,
                        "Monday breakfast",
                        1,
                        "Breakfast",
                        150,
                        MeasurementUnit.G)]),
            ]);

        Result<ShoppingListModel> result = await service.CreateAsync(request, CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(persisted);
        Assert.Multiple(
            () => Assert.Equal("Weekly groceries", persisted.Name),
            () => Assert.Equal(["First", "Second"], result.Value.Items.Select(static item => item.Name)),
            () => Assert.False(result.Value.Items[0].IsChecked),
            () => Assert.Equal("Monday breakfast", Assert.Single(result.Value.Items[0].Sources).Label),
            () => Assert.Equal(mealPlanId.Value, result.Value.Items[0].Sources[0].MealPlanId),
            () => Assert.Equal(recipeId.Value, result.Value.Items[0].Sources[0].RecipeId));
        await repository.Received(1).AddAsync(persisted, CancellationToken.None);
    }
}
