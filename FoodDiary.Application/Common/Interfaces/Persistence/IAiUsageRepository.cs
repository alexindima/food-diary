using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Models;
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

public interface IAiUsageRepository
{
    Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default);
    Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
    Task<AiUsageTotals> GetUserTotalsAsync(
        UserId userId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default);
}


