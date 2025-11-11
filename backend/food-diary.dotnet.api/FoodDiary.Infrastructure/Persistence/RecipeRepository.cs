using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class RecipeRepository : IRecipeRepository
{
    private readonly FoodDiaryDbContext _context;

    public RecipeRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<Recipe> AddAsync(Recipe recipe)
    {
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task<(IReadOnlyList<(Recipe Recipe, int UsageCount)> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        bool includePublic,
        int page,
        int limit,
        string? search,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);

        IQueryable<Recipe> query = _context.Recipes
            .AsNoTracking()
            .Include(r => r.Steps)
                .ThenInclude(s => s.Ingredients)
            .Where(includePublic
                ? r => r.UserId == userId || r.Visibility == Visibility.PUBLIC
                : r => r.UserId == userId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalized = search.Trim().ToLower();
            query = query.Where(r =>
                r.Name.ToLower().Contains(normalized) ||
                (r.Category != null && r.Category.ToLower().Contains(normalized)) ||
                (r.Description != null && r.Description.ToLower().Contains(normalized)));
        }

        var orderedQuery = query.OrderByDescending(r => r.CreatedOnUtc);
        var totalItems = await orderedQuery.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new
            {
                Recipe = r,
                UsageCount = r.MealItems.Count + r.NestedRecipeUsages.Count
            })
            .ToListAsync(cancellationToken);

        return (items.Select(i => (i.Recipe, i.UsageCount)).ToList(), totalItems);
    }

    public async Task<Recipe?> GetByIdAsync(
        RecipeId id,
        UserId userId,
        bool includePublic = true,
        bool includeSteps = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Recipe> query = _context.Recipes;

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        if (includeSteps)
        {
            query = query
                .Include(r => r.Steps)
                    .ThenInclude(s => s.Ingredients)
                        .ThenInclude(i => i.Product)
                .Include(r => r.Steps)
                    .ThenInclude(s => s.Ingredients)
                        .ThenInclude(i => i.NestedRecipe);
        }

        query = query
            .Include(r => r.MealItems)
            .Include(r => r.NestedRecipeUsages);

        return await query.FirstOrDefaultAsync(
            r => r.Id == id && (includePublic
                ? r.UserId == userId || r.Visibility == Visibility.PUBLIC
                : r.UserId == userId),
            cancellationToken);
    }

    public async Task UpdateAsync(Recipe recipe)
    {
        _context.Recipes.Update(recipe);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Recipe recipe)
    {
        _context.Recipes.Remove(recipe);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateNutritionAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        var entry = _context.Entry(recipe);
        if (entry.State == EntityState.Detached)
        {
            _context.Attach(recipe);
            entry = _context.Entry(recipe);
        }

        entry.Property(r => r.TotalCalories).IsModified = true;
        entry.Property(r => r.TotalProteins).IsModified = true;
        entry.Property(r => r.TotalFats).IsModified = true;
        entry.Property(r => r.TotalCarbs).IsModified = true;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
