using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Usda.Infrastructure.Persistence;

internal sealed class UsdaFoodRepository(DbSet<UsdaFood> foods, DbSet<UsdaFoodNutrient> foodNutrients,
    DbSet<UsdaFoodPortion> foodPortions, DbSet<DailyReferenceValue> referenceValues) : IUsdaFoodRepository {
    public async Task<IReadOnlyList<UsdaFood>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) {
        return await foods
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
        return await ProjectReadModels(foods
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
        return await foods
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.FdcId == fdcId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<UsdaFoodReadModel?> GetByFdcIdReadModelAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await ProjectReadModels(foods
                .AsNoTracking()
                .Where(f => f.FdcId == fdcId))
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaFoodNutrient>> GetNutrientsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await foodNutrients
            .AsNoTracking()
            .Include(n => n.Nutrient)
            .Where(n => n.FdcId == fdcId)
            .OrderBy(n => n.Nutrient.Name)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaNutrientReadModel>> GetNutrientReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await foodNutrients
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
        return await foodPortions
            .AsNoTracking()
            .Where(p => p.FdcId == fdcId)
            .OrderBy(p => p.PortionDescription)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UsdaFoodPortionModel>> GetPortionReadModelsAsync(
        int fdcId,
        CancellationToken cancellationToken = default) {
        return await foodPortions
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

        List<UsdaFoodNutrient> nutrients = await foodNutrients
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

    public async Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>>
        GetNutrientReadModelsByFdcIdsAsync(
            IEnumerable<int> fdcIds,
            CancellationToken cancellationToken = default) {
        var fdcIdList = fdcIds.ToList();
        if (fdcIdList.Count == 0) {
            return new Dictionary<int, IReadOnlyList<UsdaNutrientReadModel>>();
        }

        List<(int FdcId, UsdaNutrientReadModel Nutrient)> nutrients = await foodNutrients
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
        return await referenceValues
            .AsNoTracking()
            .Where(d => d.AgeGroup == ageGroup && d.Gender == gender)
            .ToDictionaryAsync(d => d.NutrientId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel>> GetDailyReferenceValueReadModelsAsync(
        string ageGroup = "adult",
        string gender = "all",
        CancellationToken cancellationToken = default) {
        return await referenceValues
            .AsNoTracking()
            .Where(d => d.AgeGroup == ageGroup && d.Gender == gender)
            .Select(d => new UsdaDailyReferenceValueReadModel(d.NutrientId, d.Value, d.Unit))
            .ToDictionaryAsync(d => d.NutrientId, cancellationToken).ConfigureAwait(false);
    }

    private static IQueryable<UsdaFoodReadModel> ProjectReadModels(IQueryable<UsdaFood> query) =>
        query.Select(food => new UsdaFoodReadModel(food.FdcId, food.Description, food.FoodCategory));
}
