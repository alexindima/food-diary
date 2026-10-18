using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.MealPlanning.Domain.ValueObjects.Ids;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Common;
using FoodDiary.Modules.MealPlanning.Application.Abstractions.MealPlans.Models;
using FoodDiary.Modules.MealPlanning.Domain.Entities.MealPlans;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.MealPlanning;

public sealed class MealPlanCompositionReader(ICompositionReadContext context) : IMealPlanCompositionReader {
    public async Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id,
        UserId? viewerId,
        CancellationToken cancellationToken = default) {
        return await context.MealPlans
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new MealPlanReadModel(
                p.Id.Value,
                p.UserId == null ? null : p.UserId.Value.Value,
                p.Name,
                p.Description,
                p.DietType.ToString(),
                p.DurationDays,
                p.TargetCaloriesPerDay,
                p.IsCurated,
                p.Days
                    .OrderBy(d => d.DayNumber)
                    .Select(d => new MealPlanDayReadModel(
                        d.Id.Value,
                        d.DayNumber,
                        d.Meals
                            .OrderBy(m => m.MealType)
                            .Join(context.Recipes.AsNoTracking().Where(recipe => recipe.Visibility == Visibility.Public || recipe.UserId == viewerId),
                                m => m.RecipeId, recipe => recipe.Id, (m, recipe) => new MealPlanMealReadModel(
                                m.Id.Value,
                                m.MealType.ToString(),
                                m.RecipeId.Value,
                                recipe.Name,
                                m.Servings,
                                recipe.Servings > 0 ? recipe.Servings : 1,
                                recipe.TotalCalories,
                                recipe.TotalProteins,
                                recipe.TotalFats,
                                recipe.TotalCarbs))
                            .ToList()))
                    .ToList()))
            .AsSplitQuery()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<RecipeId, MealPlanRecipeSnapshot>> GetRecipeSnapshotsAsync(
        IReadOnlyCollection<RecipeId> ids, UserId? viewerId, CancellationToken cancellationToken = default) {
        if (ids.Count == 0) { return new Dictionary<RecipeId, MealPlanRecipeSnapshot>(); }
        var recipes = await context.Recipes.AsNoTracking()
            .Where(recipe => Enumerable.Contains(ids, recipe.Id) && (recipe.Visibility == Visibility.Public || recipe.UserId == viewerId))
            .Select(recipe => new { recipe.Id, recipe.Name, recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ingredients = await context.Recipes.AsNoTracking()
            .Where(recipe => Enumerable.Contains(ids, recipe.Id) && (recipe.Visibility == Visibility.Public || recipe.UserId == viewerId))
            .SelectMany(recipe => recipe.Steps.SelectMany(step => step.Ingredients)
                .Select(ingredient => new { RecipeId = recipe.Id, ingredient.ProductId, ingredient.Amount, ingredient.PublicName, ingredient.PublicUnit, ingredient.TextName, ingredient.AmountText }))
            .Where(ingredient => ingredient.ProductId.HasValue || ingredient.TextName != null)
            .GroupJoin(context.Products.AsNoTracking().Where(product => product.Visibility == Visibility.Public || product.UserId == viewerId),
                ingredient => ingredient.ProductId, product => (ProductId?)product.Id,
                (ingredient, products) => new { Ingredient = ingredient, Products = products })
            .SelectMany(row => row.Products.DefaultIfEmpty(), (row, product) => new {
                row.Ingredient.RecipeId,
                ProductId = product == null ? null : (ProductId?)product.Id,
                row.Ingredient.Amount,
                Name = product == null ? row.Ingredient.PublicName ?? row.Ingredient.TextName : product.Name,
                BaseUnit = product == null ? null : (MeasurementUnit?)product.BaseUnit,
                PublicUnit = product == null ? row.Ingredient.PublicUnit : null,
                Category = product == null ? null : product.Category,
                IsText = !row.Ingredient.ProductId.HasValue && row.Ingredient.TextName != null,
                row.Ingredient.AmountText,
            })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<RecipeId, MealPlanRecipeIngredientSnapshot> byRecipe = ingredients
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .ToLookup(item => item.RecipeId, item => new MealPlanRecipeIngredientSnapshot(item.ProductId,
                item.Amount, item.Name!, item.BaseUnit ?? ParsePublicUnit(item.PublicUnit), item.Category, item.PublicUnit) {
                IsText = item.IsText,
                AmountText = item.AmountText,
            });
        return recipes.ToDictionary(recipe => recipe.Id,
            recipe => new MealPlanRecipeSnapshot(recipe.Id, recipe.Name, recipe.Servings, byRecipe[recipe.Id].ToArray(), recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs));
    }

    private static MeasurementUnit? ParsePublicUnit(string? unit) =>
        Enum.TryParse(unit, ignoreCase: true, out MeasurementUnit parsed) &&
        string.Equals(Enum.GetName(parsed), unit, StringComparison.OrdinalIgnoreCase) ? parsed : null;
}
