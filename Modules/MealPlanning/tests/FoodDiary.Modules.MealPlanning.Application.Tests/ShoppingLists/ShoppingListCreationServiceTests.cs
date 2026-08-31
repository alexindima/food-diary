using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Common;
using FoodDiary.Application.MealPlanning.ShoppingLists.Models;
using FoodDiary.Application.MealPlanning.ShoppingLists.Services;
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
            .Returns(call => call.Arg<ShoppingList>()!);
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
            () => Assert.Equal(["First", "Second"], result.Value.Items.Select(static item => item.Name), StringComparer.Ordinal),
            () => Assert.False(result.Value.Items[0].IsChecked),
            () => Assert.Equal("Monday breakfast", Assert.Single(result.Value.Items[0].Sources).Label),
            () => Assert.Equal(mealPlanId.Value, result.Value.Items[0].Sources[0].MealPlanId),
            () => Assert.Equal(recipeId.Value, result.Value.Items[0].Sources[0].RecipeId));
        await repository.Received(1).AddAsync(persisted, CancellationToken.None);
    }

    [Fact]
    public async Task CreateAsync_RepeatedRequestCreatesIndependentListsAndPreservesSources() {
        IShoppingListWriteRepository repository = Substitute.For<IShoppingListWriteRepository>();
        var persisted = new List<ShoppingList>();
        repository.AddAsync(Arg.Do<ShoppingList>(persisted.Add), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<ShoppingList>()!);
        var service = new ShoppingListCreationService(repository);
        var request = new ShoppingListCreationRequest(UserId.New(), "Weekly",
            [new ShoppingListCreationItem(ProductId.New(), "Rice", 150, MeasurementUnit.G, Category: null, 0,
                [new ShoppingListCreationSource(MealPlanId.New(), MealPlanMealId.New(), RecipeId.New(),
                    "Lunch", 1, "Lunch", 150, MeasurementUnit.G)])]);
        using var cancellation = new CancellationTokenSource();

        ShoppingListModel first = ResultAssert.Success(await service.CreateAsync(request, cancellation.Token));
        ShoppingListModel second = ResultAssert.Success(await service.CreateAsync(request, cancellation.Token));

        Assert.Multiple(
            () => Assert.NotEqual(first.Id, second.Id),
            () => Assert.NotEqual(first.Items[0].Id, second.Items[0].Id),
            () => Assert.NotEqual(first.Items[0].Sources[0].Id, second.Items[0].Sources[0].Id),
            () => Assert.Equal(first.Items[0].Sources[0].MealPlanId, second.Items[0].Sources[0].MealPlanId),
            () => Assert.Equal(150, second.Items[0].Amount),
            () => Assert.Equal(2, persisted.Count));
        await repository.Received(2).AddAsync(Arg.Any<ShoppingList>(), cancellation.Token);
    }

}
