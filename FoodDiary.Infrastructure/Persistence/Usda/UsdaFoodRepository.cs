using FoodDiary.Application.Abstractions.Usda.Common;
using FoodDiary.Application.Abstractions.Usda.Models;
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
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaFoodReadModel>> SearchReadModelsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModels(dbContext.UsdaFoods
                .AsNoTracking()
                .Where(f => EF.Functions.ILike(f.Description, $"%{query}%"))
                .OrderBy(f => f.Description.Length)
                .ThenBy(f => f.Description))
            .Take(limit)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<UsdaFood?> GetByFdcIdAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoods
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FdcId == fdcId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UsdaFoodReadModel?> GetByFdcIdReadModelAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModels(dbContext.UsdaFoods
                .AsNoTracking()
                .Where(f => f.FdcId == fdcId))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoodNutrients
            .AsNoTracking()
            .Include(n => n.Nutrient)
            .Where(n => n.FdcId == fdcId)
            .OrderBy(n => n.Nutrient.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaNutrientReadModel>> GetNutrientReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoodNutrients
            .AsNoTracking()
            .Where(n => n.FdcId == fdcId)
            .OrderBy(n => n.Nutrient.Name)
            .Select(n => new UsdaNutrientReadModel(
                n.NutrientId,
                n.Nutrient.Name,
                n.Nutrient.UnitName,
                n.Amount))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaFoodPortion>> GetPortionsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoodPortions
            .AsNoTracking()
            .Where(p => p.FdcId == fdcId)
            .OrderBy(p => p.PortionDescription)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaFoodPortionModel>> GetPortionReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await dbContext.UsdaFoodPortions
            .AsNoTracking()
            .Where(p => p.FdcId == fdcId)
            .OrderBy(p => p.PortionDescription)
            .Select(p => new UsdaFoodPortionModel(
                p.Id,
                p.Amount,
                p.MeasureUnitName,
                p.GramWeight,
                p.PortionDescription,
                p.Modifier))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaFoodNutrient>>> GetNutrientsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default) {
        var fdcIdList = fdcIds.ToList();
        if (fdcIdList.Count == 0) {
            return new Dictionary<int, IReadOnlyList<UsdaFoodNutrient>>();
        }

        List<UsdaFoodNutrient> nutrients = await dbContext.UsdaFoodNutrients
            .AsNoTracking()
            .Include(n => n.Nutrient)
            .Where(n => fdcIdList.Contains(n.FdcId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return nutrients
            .GroupBy(n => n.FdcId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UsdaFoodNutrient>)[.. g]);
    }

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>> GetNutrientReadModelsByFdcIdsAsync(
        IEnumerable<int> fdcIds,
        CancellationToken cancellationToken = default) {
        var fdcIdList = fdcIds.ToList();
        if (fdcIdList.Count == 0) {
            return new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>>();
        }

        List<(int FdcId, UsdaNutrientReadModel Nutrient)> nutrients = await dbContext.UsdaFoodNutrients
            .AsNoTracking()
            .Where(n => fdcIdList.Contains(n.FdcId))
            .Select(n => new ValueTuple<int, UsdaNutrientReadModel>(
                n.FdcId,
                new UsdaNutrientReadModel(
                    n.NutrientId,
                    n.Nutrient.Name,
                    n.Nutrient.UnitName,
                    n.Amount)))
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return nutrients
            .GroupBy(n => n.FdcId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<UsdaNutrientReadModel>)[.. g.Select(n => n.Nutrient)]);
    }

    public async Task<IReadOnlyDictionary<int, DailyReferenceValue>> GetDailyReferenceValuesAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default) {
        return await dbContext.DailyReferenceValues
            .AsNoTracking()
            .Where(d => d.AgeGroup == ageGroup && d.Gender == gender)
            .ToDictionaryAsync(d => d.NutrientId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel>> GetDailyReferenceValueReadModelsAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default) {
        return await dbContext.DailyReferenceValues
            .AsNoTracking()
            .Where(d => d.AgeGroup == ageGroup && d.Gender == gender)
            .Select(d => new UsdaDailyReferenceValueReadModel(d.NutrientId, d.Value, d.Unit))
            .ToDictionaryAsync(d => d.NutrientId, cancellationToken).ConfigureAwait(false);
    }

    private static IQueryable<UsdaFoodReadModel> ProjectReadModels(IQueryable<UsdaFood> query) =>
        query.Select(food => new UsdaFoodReadModel(food.FdcId, food.Description, food.FoodCategory));
}
