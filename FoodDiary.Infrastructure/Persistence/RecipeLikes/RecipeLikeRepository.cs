using FoodDiary.Application.Abstractions.RecipeLikes.Common;
using FoodDiary.Domain.Entities.Social;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.RecipeLikes;

internal sealed class RecipeLikeRepository(FoodDiaryDbContext context) : IRecipeLikeRepository {
    public async Task<RecipeLike?> GetByUserAndRecipeAsync(
        UserId userId, RecipeId recipeId, CancellationToken cancellationToken = default) {
        return await context.RecipeLikes
            .AsTracking()
            .FirstOrDefaultAsync(l => l.UserId == userId && l.RecipeId == recipeId, cancellationToken);
    }

    public async Task<RecipeLike> AddAsync(RecipeLike like, CancellationToken cancellationToken = default) {
        await context.RecipeLikes.AddAsync(like, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return like;
    }

    public async Task DeleteAsync(RecipeLike like, CancellationToken cancellationToken = default) {
        context.RecipeLikes.Remove(like);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountByRecipeAsync(RecipeId recipeId, CancellationToken cancellationToken = default) {
        return await context.RecipeLikes
            .AsNoTracking()
            .CountAsync(l => l.RecipeId == recipeId, cancellationToken);
    }
}
