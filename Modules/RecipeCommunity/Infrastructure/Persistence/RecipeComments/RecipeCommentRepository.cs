using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Common;
using FoodDiary.Modules.RecipeCommunity.Application.Abstractions.RecipeComments.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Application.Contracts.Common.Validation;
using FoodDiary.Modules.RecipeCommunity.Domain.Entities.Recipes;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.RecipeCommunity.Infrastructure.Persistence.RecipeComments;

internal sealed class RecipeCommentRepository(DbSet<RecipeComment> entries, IUserCommentAuthorReadService users) : IRecipeCommentRepository {
    public async Task<RecipeComment> AddAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        await entries.AddAsync(comment, cancellationToken);
        return comment;
    }

    public async Task<RecipeComment?> GetByIdAsync(
        RecipeCommentId id, bool asTracking = false, CancellationToken cancellationToken = default) {
        IQueryable<RecipeComment> query = asTracking ? entries.AsTracking() : entries.AsNoTracking();
        return await query.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task UpdateAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        entries.Update(comment);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(RecipeComment comment, CancellationToken cancellationToken = default) {
        entries.Remove(comment);
        return Task.CompletedTask;
    }

    public async Task<(IReadOnlyList<RecipeComment> Items, int Total)> GetPagedByRecipeAsync(
        RecipeId recipeId, int page, int limit, CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<RecipeComment> query = entries
            .AsNoTracking()
            .Where(c => c.RecipeId == recipeId);

        int total = await query.CountAsync(cancellationToken);

        List<RecipeComment> items = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<RecipeCommentReadModel> Items, int Total)> GetPagedReadModelsByRecipeAsync(
        RecipeId recipeId,
        int page,
        int limit,
        CancellationToken cancellationToken = default) {
        int pageNumber = PaginationPolicy.NormalizePage(page);
        int pageSize = PaginationPolicy.NormalizePageSize(limit, defaultPageSize: 1);
        IQueryable<RecipeComment> query = entries
            .AsNoTracking()
            .Where(c => c.RecipeId == recipeId);

        int total = await query.CountAsync(cancellationToken);

        var pageItems = await query
            .OrderByDescending(c => c.CreatedOnUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new { c.Id, c.RecipeId, c.UserId, c.Text, c.CreatedOnUtc, c.ModifiedOnUtc })
            .ToListAsync(cancellationToken);

        if (pageItems.Count == 0) {
            return ([], total);
        }

        IReadOnlyDictionary<UserId, UserCommentAuthorModel> authors = await users.GetAuthorsAsync(
            pageItems.Select(c => c.UserId).Distinct().ToArray(), cancellationToken);
        RecipeCommentReadModel[] items = [.. pageItems.Where(c => authors.ContainsKey(c.UserId))
            .Select(c => new RecipeCommentReadModel(c.Id.Value, c.RecipeId.Value, c.UserId.Value,
                authors[c.UserId].Username, authors[c.UserId].FirstName, c.Text, c.CreatedOnUtc, c.ModifiedOnUtc))];

        return (items, total);
    }
}
