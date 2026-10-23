using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using System.Globalization;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.MealPlanning.Application.Common.Validation;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Common;
using FoodDiary.Modules.MealPlanning.Application.ShoppingLists.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.GenerateShoppingList;

public sealed class GenerateShoppingListCommandHandler(
    IMealPlanReadRepository mealPlanRepository,
    IShoppingListCreationService shoppingListCreationService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<GenerateShoppingListCommand, Result<ShoppingListModel>> {
    public async Task<Result<ShoppingListModel>> Handle(
        GenerateShoppingListCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ShoppingListModel>(userIdResult);
        }

        Result<MealPlanId> planIdResult = RequiredIdParser.Parse(
            command.PlanId,
            nameof(command.PlanId),
            "Meal plan id must not be empty.",
            value => new MealPlanId(value));
        if (planIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ShoppingListModel, MealPlanId>(planIdResult);
        }

        MealPlanId planId = planIdResult.Value;
        MealPlan? plan = await mealPlanRepository.GetAccessibleByIdAsync(
            planId,
            userIdResult.Value,
            includeDays: true,
            cancellationToken).ConfigureAwait(false);
        if (plan is null) {
            return Result.Failure<ShoppingListModel>(MealPlanErrors.NotFound(command.PlanId));
        }

        return await shoppingListCreationService.CreateAsync(
            CreateShoppingListRequest(userIdResult.Value, plan),
            cancellationToken).ConfigureAwait(false);
    }

    private static ShoppingListCreationRequest CreateShoppingListRequest(UserId userId, MealPlan plan) =>
        new(
            userId,
            plan.Name,
            [.. AggregateIngredients(plan)
                .OrderBy(static item => item.SortOrder)
                .Select(item => new ShoppingListCreationItem(
                    item.ProductId,
                    item.Name,
                    item.Unit.HasValue ? Math.Round(item.TotalAmount, 1, MidpointRounding.ToEven) : null,
                    item.Unit,
                    item.Category,
                    item.SortOrder,
                    [.. item.Sources.Select(source => new ShoppingListCreationSource(
                        plan.Id,
                        source.MealPlanMealId,
                        source.RecipeId,
                        source.Label,
                        source.DayNumber,
                        source.MealType,
                        Math.Round(source.Amount, 1, MidpointRounding.ToEven),
                        source.Unit))],
                    item.Note ?? (item.Unit.HasValue || string.IsNullOrWhiteSpace(item.PublishedUnit) ? null :
                        string.Create(CultureInfo.InvariantCulture, $"{Math.Round(item.TotalAmount, 1, MidpointRounding.ToEven)} {item.PublishedUnit}"))))]);

    private static IReadOnlyList<AggregatedIngredient> AggregateIngredients(MealPlan plan) {
        var aggregated = new Dictionary<(ProductId?, string?, string?), AggregatedIngredient>();
        var textItems = new List<AggregatedIngredient>();
        int sortOrder = 0;

        foreach (MealPlanDay day in plan.Days.OrderBy(d => d.DayNumber)) {
            foreach (MealPlanMeal meal in day.Meals) {
                MealPlanRecipeSnapshot? recipe = meal.RecipeSnapshot;
                if (recipe is null) {
                    continue;
                }

                double servingsMultiplier = recipe.Servings > 0
                    ? (double)meal.Servings / recipe.Servings
                    : 1.0;

                foreach (MealPlanRecipeIngredientSnapshot ingredient in recipe.Ingredients) {
                    if (ingredient.IsText) {
                        // Literal amounts have no numeric basis; preserve each use without scaling or summing it.
                        textItems.Add(new AggregatedIngredient {
                            ProductId = null,
                            Name = ingredient.Name,
                            Note = ingredient.AmountText,
                            TotalAmount = 0,
                            SortOrder = sortOrder++,
                            Sources = [],
                        });
                        continue;
                    }

                    ProductId? productId = ingredient.ProductId;
                    (ProductId?, string?, string?) key = (productId, productId.HasValue ? null : ingredient.Name, ingredient.BaseUnit?.ToString() ?? ingredient.PublishedUnit);
                    double scaledAmount = ingredient.Amount * servingsMultiplier;

                    var source = new IngredientSource {
                        MealPlanMealId = meal.Id,
                        RecipeId = recipe.Id,
                        Label = recipe.Name,
                        DayNumber = day.DayNumber,
                        MealType = meal.MealType.ToString(),
                        Amount = scaledAmount,
                        Unit = ingredient.BaseUnit,
                    };

                    if (aggregated.TryGetValue(key, out AggregatedIngredient? existing)) {
                        existing.TotalAmount += scaledAmount;
                        existing.Sources.Add(source);
                    } else {
                        aggregated[key] = new AggregatedIngredient {
                            ProductId = productId,
                            Name = ingredient.Name,
                            Unit = ingredient.BaseUnit,
                            PublishedUnit = ingredient.PublishedUnit,
                            Category = ingredient.Category,
                            TotalAmount = scaledAmount,
                            SortOrder = sortOrder++,
                            Sources = [source],
                        };
                    }
                }
            }
        }

        return [.. aggregated.Values, .. textItems];
    }

    private sealed class AggregatedIngredient {
        public required ProductId? ProductId { get; init; }
        public required string Name { get; init; }
        public MeasurementUnit? Unit { get; init; }
        public string? PublishedUnit { get; init; }
        public string? Note { get; init; }
        public string? Category { get; init; }
        public double TotalAmount { get; set; }
        public int SortOrder { get; init; }
        public required List<IngredientSource> Sources { get; init; }
    }

    private sealed class IngredientSource {
        public required MealPlanMealId MealPlanMealId { get; init; }
        public required RecipeId RecipeId { get; init; }
        public required string Label { get; init; }
        public required int DayNumber { get; init; }
        public required string MealType { get; init; }
        public required double Amount { get; init; }
        public MeasurementUnit? Unit { get; init; }
    }
}
