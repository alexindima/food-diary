using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Usda.Application.Mappings;
using FoodDiary.Modules.Usda.Application.Abstractions.Common;
using FoodDiary.Modules.Usda.Contracts.Common;
using FoodDiary.Modules.Usda.Contracts.Models;
using FoodDiary.Modules.Usda.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Usda.Application.Queries.GetDailyMicronutrients;

public sealed class GetDailyMicronutrientsQueryHandler(
    IUsdaMealNutritionReadService mealProductNutritionReadService,
    IUsdaFoodReadModelRepository usdaFoodRepository,
    ICurrentUserAccessService currentUserAccessService,
    IUsdaFoodSearchService provider,
    TimeProvider timeProvider)
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
    public const int MaximumProviderFoodsPerRequest = 20;
    private static readonly TimeSpan ProviderLookupBudget = TimeSpan.FromSeconds(15);

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
            .Where(static item => item.UsdaFdcId.HasValue && item.ProductBaseUnit == MeasurementUnit.G)
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

        int[] providerFdcIds = [.. fdcIds.Where(fdcId => !nutrientsByFdcId.ContainsKey(fdcId))];
        if (providerFdcIds.Length > MaximumProviderFoodsPerRequest) {
            return Result.Failure<DailyMicronutrientSummaryModel>(
                UsdaErrors.ProviderLookupLimitExceeded(MaximumProviderFoodsPerRequest));
        }
        if (providerFdcIds.Length > 0) {
            using var budget = new CancellationTokenSource(ProviderLookupBudget, timeProvider);
            using var lookupCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, budget.Token);
            try {
                nutrientsByFdcId = await ResolveProviderNutrientsAsync(providerFdcIds, nutrientsByFdcId, lookupCancellation.Token).ConfigureAwait(false);
                lookupCancellation.Token.ThrowIfCancellationRequested();
            } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && budget.IsCancellationRequested) {
                return Result.Failure<DailyMicronutrientSummaryModel>(UsdaErrors.ProviderLookupTimedOut());
            }
        }

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

    private async Task<IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>>> ResolveProviderNutrientsAsync(
        IReadOnlyList<int> fdcIds,
        IReadOnlyDictionary<int, IReadOnlyList<UsdaNutrientReadModel>> localNutrients,
        CancellationToken cancellationToken) {
        var resolved = localNutrients.ToDictionary(static item => item.Key, static item => item.Value);
        foreach (int fdcId in fdcIds) {
            if (resolved.ContainsKey(fdcId)) {
                continue;
            }
            UsdaFoodDetailModel? detail = await provider.GetFoodDetailAsync(fdcId, cancellationToken).ConfigureAwait(false);
            if (detail is null || detail.FdcId != fdcId) {
                continue;
            }
            resolved[fdcId] = detail.Nutrients
                .Where(static nutrient => double.IsFinite(nutrient.AmountPer100G) && nutrient.AmountPer100G >= 0)
                .GroupBy(static nutrient => nutrient.NutrientId)
                .Select(static group => group.First())
                .Select(static nutrient => new UsdaNutrientReadModel(nutrient.NutrientId, nutrient.Name, nutrient.Unit, nutrient.AmountPer100G))
                .ToList();
        }
        return resolved;
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

            // USDA reference values are per 100 grams, independent of product nutrition settings.
            double scale = item.Amount / 100d;

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
