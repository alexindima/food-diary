using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Recipes;

namespace FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;

public class GenerateShoppingListCommandHandler(
    IMealPlanRepository mealPlanRepository,
    IShoppingListRepository shoppingListRepository)
    : ICommandHandler<GenerateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GenerateShoppingListCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(userIdResult.Error);
        }

        var planId = new MealPlanId(command.PlanId);
        MealPlan? plan = await mealPlanRepository.GetByIdAsync(planId, includeDays: true, cancellationToken).ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<ShoppingListModel>(Errors.MealPlan.NotFound(command.PlanId));
        }

        if (!plan.IsCurated && plan.UserId != userIdResult.Value) {
            return Result.Failure<ShoppingListModel>(Errors.MealPlan.NotFound(command.PlanId));
        }

        ShoppingList shoppingList = CreateShoppingList(userIdResult.Value, plan);
        await shoppingListRepository.AddAsync(shoppingList, cancellationToken).ConfigureAwait(false);

        return Result.Success(shoppingList.ToModel());
    }

    private static ShoppingList CreateShoppingList(UserId userId, MealPlan plan) {
        var shoppingList = ShoppingList.Create(userId, plan.Name);
        foreach (AggregatedIngredient? item in AggregateIngredients(plan).Values.OrderBy(i => i.SortOrder)) {
            shoppingList.AddItem(
                item.Name,
                item.ProductId,
                Math.Round(item.TotalAmount, 1, MidpointRounding.ToEven),
                item.Unit,
                item.Category,
                isChecked: false,
                item.SortOrder);
        }

        return shoppingList;
    }

    private static Dictionary<ProductId, AggregatedIngredient> AggregateIngredients(MealPlan plan) {
        var aggregated = new Dictionary<ProductId, AggregatedIngredient>();
        int sortOrder = 0;

        foreach (MealPlanDay? day in plan.Days.OrderBy(d => d.DayNumber)) {
            foreach (MealPlanMeal meal in day.Meals) {
                Recipe? recipe = meal.Recipe;
                if (recipe is null) {
                    continue;
                }

                double servingsMultiplier = recipe.Servings > 0
                    ? (double)meal.Servings / recipe.Servings
                    : 1.0;

                foreach (RecipeStep step in recipe.Steps) {
                    foreach (RecipeIngredient ingredient in step.Ingredients) {
                        if (ingredient.ProductId is null || ingredient.Product is null) {
                            continue;
                        }

                        ProductId productId = ingredient.ProductId.Value;
                        double scaledAmount = ingredient.Amount * servingsMultiplier;

                        if (aggregated.TryGetValue(productId, out AggregatedIngredient? existing)) {
                            existing.TotalAmount += scaledAmount;
                        } else {
                            aggregated[productId] = new AggregatedIngredient {
                                ProductId = productId,
                                Name = ingredient.Product.Name,
                                Unit = ingredient.Product.BaseUnit,
                                Category = ingredient.Product.Category,
                                TotalAmount = scaledAmount,
                                SortOrder = sortOrder++,
                            };
                        }
                    }
                }
            }
        }

        return aggregated;
    }

    private sealed class AggregatedIngredient {
        public required ProductId ProductId { get; init; }
        public required string Name { get; init; }
        public Domain.Enums.MeasurementUnit? Unit { get; init; }
        public string? Category { get; init; }
        public double TotalAmount { get; set; }
        public int SortOrder { get; init; }
    }
}
