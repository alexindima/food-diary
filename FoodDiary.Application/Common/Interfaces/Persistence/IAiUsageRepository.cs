using FoodDiary.Application.Admin.Models;
using FoodDiary.Domain.Entities;

namespace FoodDiary.Application.Common.Interfaces.Persistence;

public interface IAiUsageRepository
{
    Task AddAsync(AiUsage usage, CancellationToken cancellationToken = default);
    Task<AiUsageSummary> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default);
}
