using FoodDiary.Application.Abstractions.MealPlans.Common;
using FoodDiary.Application.Abstractions.MealPlans.Models;
using FoodDiary.Domain.Entities.MealPlans;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.ReadModel.Composition.MealPlanning;

public sealed class MealPlanCompositionReader(ICompositionReadContext context) : IMealPlanCompositionReader {
    public async Task<MealPlanReadModel?> GetReadModelByIdAsync(
        MealPlanId id,
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
                            .Join(context.Recipes.AsNoTracking(), m => m.RecipeId, recipe => recipe.Id, (m, recipe) => new MealPlanMealReadModel(
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
        IReadOnlyCollection<RecipeId> ids, CancellationToken cancellationToken = default) {
        if (ids.Count == 0) { return new Dictionary<RecipeId, MealPlanRecipeSnapshot>(); }
        var recipes = await context.Recipes.AsNoTracking()
            .Where(recipe => Enumerable.Contains(ids, recipe.Id))
            .Select(recipe => new { recipe.Id, recipe.Name, recipe.Servings, recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var ingredients = await context.Recipes.AsNoTracking()
            .Where(recipe => Enumerable.Contains(ids, recipe.Id))
            .SelectMany(recipe => recipe.Steps.SelectMany(step => step.Ingredients)
                .Select(ingredient => new { RecipeId = recipe.Id, ingredient.ProductId, ingredient.Amount }))
            .Join(context.Products.AsNoTracking(), ingredient => ingredient.ProductId, product => (ProductId?)product.Id,
                (ingredient, product) => new {
                    ingredient.RecipeId,
                    Ingredient = new MealPlanRecipeIngredientSnapshot(product.Id, ingredient.Amount, product.Name, product.BaseUnit, product.Category),
                })
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        ILookup<RecipeId, MealPlanRecipeIngredientSnapshot> byRecipe = ingredients.ToLookup(item => item.RecipeId, item => item.Ingredient);
        return recipes.ToDictionary(recipe => recipe.Id,
            recipe => new MealPlanRecipeSnapshot(recipe.Id, recipe.Name, recipe.Servings, byRecipe[recipe.Id].ToArray(), recipe.TotalCalories, recipe.TotalProteins, recipe.TotalFats, recipe.TotalCarbs));
    }
}
