using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Domain.Entities.Usda;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Usda;

internal sealed class UsdaFoodRepository(FoodDiaryDbContext dbContext) : IUsdaFoodRepository {
    public async Task<IReadOnlyList<UsdaFood>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoods
            .AsNoTracking()
            .Where(f => EF.Functions.ILike(f.Description, $"%{query}%"))
            .OrderBy(f => f.Description.Length)
            .ThenBy(f => f.Description)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<UsdaFood?> GetByFdcIdAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoods
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FdcId == fdcId, cancellationToken);
    }

    public async Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoodNutrients
            .AsNoTracking()
            .Include(n => n.Nutrient)
            .Where(n => n.FdcId == fdcId)
            .OrderBy(n => n.Nutrient.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoodPortions
            .AsNoTracking()
            .Where(p => p.FdcId == fdcId)
            .OrderBy(p => p.PortionDescription)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default) {
        var fdcIdList = fdcIds.ToList();
        if (fdcIdList.Count == 0) {
            return new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>();
        }

        var nutrients = await dbContext.UsdaFoodNutrients
            .AsNoTracking()
            .Include(n => n.Nutrient)
            .Where(n => fdcIdList.Contains(n.FdcId))
            .ToListAsync(cancellationToken);

        return nutrients
            .GroupBy(n => n.FdcId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UsdaFoodNutrient>)g.ToList());
    }

    public async Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default) {
        return await dbContext.DailyReferenceValues
            .AsNoTracking()
            .Where(d => d.AgeGroup == ageGroup && d.Gender == gender)
            .ToDictionaryAsync(d => d.NutrientId, cancellationToken);
    }
}
