using FoodDiary.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.RecipeComments;

internal sealed class RecipeCommentRepository(FoodDiaryDbContext context, IUserCommentAuthorReadService users) : IRecipeCommentRepository {
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
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<RecipeComment> query = context.RecipeComments
            .AsNoTracking()
            .Where(c => c.RecipeId == recipeId);

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        List<RecipeComment> items = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return (items, total);
    }

    public async Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<RecipeComment> query = context.RecipeComments
            .AsNoTracking()
            .Where(c => c.RecipeId == recipeId);

        int total = await query.CountAsync(cancellationToken).ConfigureAwait(false);

        var pageItems = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new { c.Id, c.RecipeId, c.UserId, c.Text, c.CreatedOnUtc, c.ModifiedOnUtc })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        if (pageItems.Count == 0) {
            return ([], total);
        }

        IReadOnlyDictionary<UserId, UserCommentAuthorModel> authors = await users.GetAuthorsAsync(
            pageItems.Select(c => c.UserId).Distinct().ToArray(), cancellationToken).ConfigureAwait(false);
        RecipeCommentReadModel[] items = [.. pageItems.Where(c => authors.ContainsKey(c.UserId))
            .Select(c => new RecipeCommentReadModel(c.Id.Value, c.RecipeId.Value, c.UserId.Value,
                authors[c.UserId].Username, authors[c.UserId].FirstName, c.Text, c.CreatedOnUtc, c.ModifiedOnUtc))];

        return (items, total);
    }
}
