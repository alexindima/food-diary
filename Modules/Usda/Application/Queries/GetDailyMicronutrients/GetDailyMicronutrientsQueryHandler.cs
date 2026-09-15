using FoodDiary.Modules.Usda.Application.Mappings;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;

public sealed class GetDailyMicronutrientsQueryHandler(
    IUsdaMealNutritionReadService mealProductNutritionReadService,
    IUsdaFoodReadModelRepository usdaFoodRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetDailyMicronutrientsQuery, Result<DailyMicronutrientSummaryModel>> {
    public async Task<Result<DailyMicronutrientSummaryModel>> Handle(
        GetDailyMicronutrientsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<DailyMicronutrientSummaryModel>(userIdResult);
        }

        return await GetDailySummaryAsync(
            userIdResult.Value,
            query.Date,
            cancellationToken).ConfigureAwait(false);
    }
    public const int MaximumProductItemsPerDay = 1000;

    private async Task<Result<DailyMicronutrientSummaryModel>> GetDailySummaryAsync(
        UserId userId,
        DateTime date,
        CancellationToken cancellationToken) {
        IReadOnlyList<UsdaMealProductNutritionReadModel> productItems = await mealProductNutritionReadService.GetForDateAsync(
            userId,
            date,
            MaximumProductItemsPerDay + 1,
            cancellationToken).ConfigureAwait(false);

        if (productItems.Count > MaximumProductItemsPerDay) {
            return Result.Failure<DailyMicronutrientSummaryModel>(
                UsdaErrors.DailyMicronutrientItemLimitExceeded(MaximumProductItemsPerDay));
        }

        var linkedItems = productItems
            .Where(static item => item.UsdaFdcId.HasValue)
            .ToList();

        int totalProductCount = productItems.Count;
        int linkedProductCount = linkedItems.Count;

        if (linkedItems.Count == 0) {
            return Result.Success(new DailyMicronutrientSummaryModel(date, 0, totalProductCount, [], HealthScores: null));
        }

        var fdcIds = linkedItems
            .Select(static item => item.UsdaFdcId!.Value)
            .Distinct()
            .ToList();

        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrientsByFdcId = await usdaFoodRepository
            .GetNutrientReadModelsByFdcIdsAsync(fdcIds, cancellationToken)
            .ConfigureAwait(false);
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues = await usdaFoodRepository
            .GetDailyReferenceValueReadModelsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        Dictionary<int, AggregatedNutrient> aggregated = AggregateNutrients(linkedItems, nutrientsByFdcId);
        List<DailyMicronutrientModel> nutrientModels = BuildNutrientModels(aggregated, dailyValues);
        var nutrientAmounts = aggregated.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value.Total);
        var dvAmounts = dailyValues.ToDictionary(static kvp => kvp.Key, static kvp => kvp.Value.Value);
        var healthScores = HealthAreaScores.Calculate(nutrientAmounts, dvAmounts);

        return Result.Success(new DailyMicronutrientSummaryModel(
            date,
            linkedProductCount,
            totalProductCount,
            nutrientModels,
            healthScores.ToModel()));
    }

    private static Dictionary<int, AggregatedNutrient> AggregateNutrients(
        IReadOnlyList<UsdaMealProductNutritionReadModel> linkedItems,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> nutrientsByFdcId) {
        var aggregated = new Dictionary<int, AggregatedNutrient>();
        foreach (UsdaMealProductNutritionReadModel item in linkedItems) {
            int fdcId = item.UsdaFdcId!.Value;

            if (!nutrientsByFdcId.TryGetValue(fdcId, out IReadOnlyList<UsdaNutrientReadModel>? nutrients)) {
                continue;
            }

            double scale = item.ProductBaseAmount > 0 ? item.Amount / item.ProductBaseAmount : 0;

            foreach (UsdaNutrientReadModel nutrient in nutrients) {
                double scaledAmount = nutrient.Amount * scale;
                if (aggregated.TryGetValue(nutrient.NutrientId, out AggregatedNutrient? existing)) {
                    aggregated[nutrient.NutrientId] = existing with { Total = existing.Total + scaledAmount };
                } else {
                    aggregated[nutrient.NutrientId] = new AggregatedNutrient(nutrient.Name, nutrient.Unit, scaledAmount);
                }
            }
        }

        return aggregated;
    }

    private static List<DailyMicronutrientModel> BuildNutrientModels(
        IReadOnlyDictionary<int, AggregatedNutrient> aggregated,
        IReadOnlyDictionary<int, UsdaDailyReferenceValueReadModel> dailyValues) {
        return [.. aggregated
            .Select(kvp => {
                dailyValues.TryGetValue(kvp.Key, out UsdaDailyReferenceValueReadModel? drv);
                double? dv = drv?.Value;
                double? percentDv = dv is > 0 ? Math.Round(kvp.Value.Total / dv.Value * 100, 1, MidpointRounding.ToEven) : null;

                return new DailyMicronutrientModel(
                    kvp.Key,
                    kvp.Value.Name,
                    kvp.Value.Unit,
                    Math.Round(kvp.Value.Total, 2, MidpointRounding.ToEven),
                    dv,
                    percentDv);
            })
            .OrderBy(static nutrient => nutrient.Name, StringComparer.Ordinal)];
    }

    private sealed record AggregatedNutrient(string Name, string Unit, double Total);
}
