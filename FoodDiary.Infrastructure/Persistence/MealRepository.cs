using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities;
using FoodDiary.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public class MealRepository : IMealRepository
{
    private readonly FoodDiaryDbContext _context;

    public MealRepository(FoodDiaryDbContext context) => _context = context;

    public async Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        await _context.Meals.AddAsync(meal, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return meal;
    }

    public async Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        _context.Meals.Update(meal);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default)
    {
        _context.Meals.Remove(meal);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Meal?> GetByIdAsync(
        MealId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Meal> query = _context.Meals;

        if (includeItems)
        {
            query = query
                .Include(m => m.Items)
                    .ThenInclude(i => i.Product)
                .Include(m => m.Items)
                    .ThenInclude(i => i.Recipe);
        }

        if (!asTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(
            m => m.Id == id && m.UserId == userId,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(page, 1);
        var pageSize = Math.Max(limit, 1);

        IQueryable<Meal> query = _context.Meals
            .AsNoTracking()
            .Include(m => m.Items)
                .ThenInclude(i => i.Product)
            .Include(m => m.Items)
                .ThenInclude(i => i.Recipe)
            .Where(m => m.UserId == userId);

        if (dateFrom.HasValue)
        {
            query = query.Where(m => m.Date >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(m => m.Date <= dateTo.Value);
        }

        var orderedQuery = query
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.CreatedOnUtc);

        var totalItems = await orderedQuery.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var items = await orderedQuery
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalItems);
    }

    public async Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        return await _context.Meals
            .AsNoTracking()
            .Where(m => m.UserId == userId && m.Date >= dateFrom && m.Date <= dateTo)
            .OrderBy(m => m.Date)
            .ThenBy(m => m.CreatedOnUtc)
            .ToListAsync(cancellationToken);
    }
}
