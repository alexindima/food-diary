using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.RecipeComments;

internal sealed class RecipeCommentRepository(FoodDiaryDbContext context) : IRecipeCommentRepository {
    public async Task<RecipeComment> AddAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        await context.RecipeComments.AddAsync(comment, cancellationToken).ConfigureAwait(false);
        return comment;
    }

    public async Task<RecipeComment?> GetByIdAsync(
        RecipeCommentId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<RecipeComment> query = asTracking ? context.RecipeComments.AsTracking() : context.RecipeComments.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken).ConfigureAwait(false);
    }

    public Task UpdateAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        context.RecipeComments.Update(comment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        context.RecipeComments.Remove(comment);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
        RecipeId recipeId, int page, int limit, CancellationToken cancellationToken = default) {
        IQueryable<RecipeComment> query = context.RecipeComments
            .AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.RecipeId == recipeId);

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<RecipeComment> items = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        IQueryable<RecipeComment> query = context.RecipeComments
            .AsNoTracking()
            .Where(c => c.RecipeId == recipeId);

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<RecipeCommentReadModel> items = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(c => new RecipeCommentReadModel(
                c.Id.Value,
                c.RecipeId.Value,
                c.UserId.Value,
                c.User == null ? null : c.User.Username,
                c.User == null ? null : c.User.FirstName,
                c.Text,
                c.CreatedOnUtc,
                c.ModifiedOnUtc))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }
}
