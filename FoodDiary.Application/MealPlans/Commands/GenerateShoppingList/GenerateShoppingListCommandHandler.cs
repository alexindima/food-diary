using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.ShoppingLists.Common;
using FoodDiary.Application.ShoppingLists.Mappings;
using FoodDiary.Application.ShoppingLists.Models;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.MealPlans.Commands.GenerateShoppingList;

public class GenerateShoppingListCommandHandler(
    IMealPlanRepository mealPlanRepository,
    IShoppingListRepository shoppingListRepository)
    : ICommandHandler<GenerateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GenerateShoppingListCommand command,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(command.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<ShoppingListModel>(userIdResult.Error);
        }

        var planId = new MealPlanId(command.PlanId);
        var plan = await mealPlanRepository.GetByIdAsync(planId, includeDays: true, cancellationToken);
        if (plan is null) {
            return Result.Failure<ShoppingListModel>(Errors.MealPlan.NotFound(command.PlanId));
        }

        if (!plan.IsCurated && plan.UserId != userIdResult.Value) {
            return Result.Failure<ShoppingListModel>(Errors.MealPlan.NotFound(command.PlanId));
        }

        // Aggregate all product ingredients from all recipes in the plan
        var aggregated = new Dictionary<ProductId, AggregatedIngredient>();
        var sortOrder = 0;

        foreach (var day in plan.Days.OrderBy(d => d.DayNumber)) {
            foreach (var meal in day.Meals) {
                var recipe = meal.Recipe;
                if (recipe is null) {
                    continue;
                }

                var servingsMultiplier = recipe.Servings > 0
                    ? (double)meal.Servings / recipe.Servings
                    : 1.0;

                foreach (var step in recipe.Steps) {
                    foreach (var ingredient in step.Ingredients) {
                        if (ingredient.ProductId is null || ingredient.Product is null) {
                            continue;
                        }

                        var productId = ingredient.ProductId.Value;
                        var scaledAmount = ingredient.Amount * servingsMultiplier;

                        if (aggregated.TryGetValue(productId, out var existing)) {
                            existing.TotalAmount += scaledAmount;
                        } else {
                            aggregated[productId] = new AggregatedIngredient {
                                ProductId = productId,
                                Name = ingredient.Product.Name,
                                Unit = ingredient.Product.BaseUnit,
                                Category = ingredient.Product.Category,
                                TotalAmount = scaledAmount,
                                SortOrder = sortOrder++
                            };
                        }
                    }
                }
            }
        }

        var shoppingList = ShoppingList.Create(userIdResult.Value, plan.Name);
        foreach (var item in aggregated.Values.OrderBy(i => i.SortOrder)) {
            shoppingList.AddItem(
                item.Name,
                item.ProductId,
                Math.Round(item.TotalAmount, 1),
                item.Unit,
                item.Category,
                isChecked: false,
                item.SortOrder);
        }

        await shoppingListRepository.AddAsync(shoppingList, cancellationToken);

        return Result.Success(shoppingList.ToModel());
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
