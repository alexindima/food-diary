using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.RecipeComments;

internal sealed class RecipeCommentRepository(FoodDiaryDbContext context) : IRecipeCommentRepository {
    public async Task<RecipeComment> AddAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        await context.RecipeComments.AddAsync(comment, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return comment;
    }

    public async Task<RecipeComment?> GetByIdAsync(
        RecipeCommentId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        var query = asTracking ? context.RecipeComments.AsTracking() : context.RecipeComments.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        context.RecipeComments.Update(comment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        context.RecipeComments.Remove(comment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
        RecipeId recipeId, int page, int limit, CancellationToken cancellationToken = default) {
        var query = context.RecipeComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.RecipeId == recipeId);

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return (items, total);
    }
}
