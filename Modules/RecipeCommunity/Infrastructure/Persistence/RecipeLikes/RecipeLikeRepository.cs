using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Social;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.RecipeCommunity.Infrastructure.Persistence.RecipeLikes;

internal sealed class RecipeLikeRepository(DbSet<RecipeLike> entries) : IRecipeLikeRepository {
    public async Task<RecipeLike?> GetByUserAndRecipeAsync(
        UserId userId, RecipeId recipeId, CancellationToken cancellationToken = default) {
        return await entries
            .AsTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId && l.RecipeId == recipeId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ExistsByUserAndRecipeAsync(
        UserId userId,
        RecipeId recipeId,
        CancellationToken cancellationToken = default) {
        return await entries
            .AsNoTracking()
            .AnyAsync(l => l.UserId == userId && l.RecipeId == recipeId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RecipeLike> AddAsync(RecipeLike like, CancellationToken cancellationToken = default) {
        await entries.AddAsync(like, cancellationToken).ConfigureAwait(false);
        return like;
    }

    public Task DeleteAsync(RecipeLike like, CancellationToken cancellationToken = default) {
        entries.Remove(like);
        return Task.CompletedTask;
    }

    public async Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken cancellationToken = default) {
        return await entries
            .AsNoTracking()
            .CountAsync(l => l.RecipeId == recipeId, cancellationToken).ConfigureAwait(false);
    }
}
