using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Application.Abstractions.Common;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Persistence.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Recipes.Infrastructure.Persistence;

internal sealed class RecipesUserContentRetentionParticipant(
    RecipesDbContext context,
    IModuleTransactionCoordinator coordinator,
    IRecipeUserPurgeRetentionReadService retentionReadService) : IUserContentRetentionParticipant {
    public int Order => 7;

    public async Task LockOwnedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        FormattableString sql = $"""
            SELECT "Id" AS "Value"
            FROM "Recipes"
            WHERE "UserId" = {sourceUserId.Value}
            ORDER BY "Id"
            FOR UPDATE
            """;
        _ = await context.Database.SqlQuery<Guid>(sql).ToArrayAsync(cancellationToken);
    }

    public async Task<bool> HasReferencedContentAsync(UserId sourceUserId, CancellationToken cancellationToken) {
        IReadOnlyCollection<RecipeId> recipeIds = await retentionReadService
            .GetRetainedRecipeIdsAsync(sourceUserId, cancellationToken);
        return recipeIds.Count > 0;
    }

    public async Task RetainReferencedContentAsync(UserId sourceUserId, UserId targetUserId, CancellationToken cancellationToken) {
        await context.Database.UseTransactionAsync(coordinator.CurrentTransaction, cancellationToken);
        IReadOnlyCollection<RecipeId> recipeIds = await retentionReadService
            .GetRetainedRecipeIdsAsync(sourceUserId, cancellationToken);
        if (recipeIds.Count == 0) {
            return;
        }

        IQueryable<Recipe> recipes = context.Recipes.Where(recipe => recipe.UserId == sourceUserId && recipeIds.Contains(recipe.Id));
        IQueryable<RecipeStep> steps = context.RecipeSteps.Where(step => step.Recipe.UserId == sourceUserId && recipeIds.Contains(step.RecipeId));
        Guid[] retainedIds = [.. recipeIds.Select(id => id.Value)];
        // Keep only the numeric composition needed by surviving references. Personal
        // captions and images remain owned by the erased account and are purged later.
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "RecipeStepImages" WHERE "RecipeStepId" IN
            (SELECT s."Id" FROM "RecipeSteps" s JOIN "Recipes" r ON r."Id" = s."RecipeId"
             WHERE r."UserId" = {sourceUserId.Value} AND r."Id" = ANY({retainedIds}))
            """, cancellationToken);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM "RecipeImages" WHERE "RecipeId" IN
            (SELECT "Id" FROM "Recipes" WHERE "UserId" = {sourceUserId.Value} AND "Id" = ANY({retainedIds}))
            """, cancellationToken);
        await steps.ExecuteUpdateAsync(setters => setters
            .SetProperty(step => step.Title, (string?)null)
            .SetProperty(step => step.Instruction, "Retained nutritional composition")
            .SetProperty(step => step.ImageUrl, (string?)null)
            .SetProperty(step => step.ImageAssetId, (ImageAssetId?)null), cancellationToken);
        await context.RecipeIngredients.Where(ingredient => ingredient.RecipeStep.Recipe.UserId == sourceUserId && recipeIds.Contains(ingredient.RecipeStep.RecipeId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ingredient => ingredient.PublicName, (string?)null)
                .SetProperty(ingredient => ingredient.PublicUnit, (string?)null)
                .SetProperty(ingredient => ingredient.TextName, ingredient => ingredient.TextName == null ? null : "Retained ingredient")
                .SetProperty(ingredient => ingredient.AmountText, (string?)null), cancellationToken);
        await recipes.ExecuteUpdateAsync(setters => setters
            .SetProperty(recipe => recipe.Name, "Retained recipe")
            .SetProperty(recipe => recipe.Description, (string?)null)
            .SetProperty(recipe => recipe.Comment, (string?)null)
            .SetProperty(recipe => recipe.ImageUrl, (string?)null)
            .SetProperty(recipe => recipe.ImageAssetId, (ImageAssetId?)null)
            .SetProperty(recipe => recipe.UserId, targetUserId), cancellationToken);
    }
}
