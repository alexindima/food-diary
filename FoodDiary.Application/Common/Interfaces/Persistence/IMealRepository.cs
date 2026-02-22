using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IMealRepository
{
    Task<Meal> AddAsync(Meal meal, CancellationToken cancellationToken = default);

    Task UpdateAsync(Meal meal, CancellationToken cancellationToken = default);

    Task DeleteAsync(Meal meal, CancellationToken cancellationToken = default);

    Task<Meal?> GetByIdAsync(
        MealId id,
        UserId userId,
        bool includeItems = false,
        bool asTracking = false,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Meal> Items, int TotalItems)> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Meal>> GetByPeriodAsync(
        UserId userId,
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);
}


