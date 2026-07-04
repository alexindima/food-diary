using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.FavoriteMeals;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;

public sealed class GetConsumptionsOverviewQueryHandler(
    IMealReadRepository mealRepository,
    ICurrentUserAccessService currentUserAccessService,
    IFavoriteMealReadRepository favoriteMealRepository)
    : IQueryHandler<GetConsumptionsOverviewQuery, Result<ConsumptionOverviewModel>> {
    public async Task<Result<ConsumptionOverviewModel>> Handle(
        GetConsumptionsOverviewQuery request,
        CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(request.UserId.Value);
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<ConsumptionOverviewModel>(accessError);
        }

        int sanitizedPage = Math.Max(request.Page, 1);
        int sanitizedLimit = Math.Clamp(request.Limit, 1, 100);
        int favoriteLimit = Math.Clamp(request.FavoriteLimit, 1, 50);
        DateTime? normalizedFrom = request.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom.Value)
            : null;
        DateTime? normalizedTo = request.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo.Value)
            : null;
        MealQueryFilters filters = CreateFilters(request, normalizedFrom, normalizedTo);

        (IReadOnlyList<Meal> Items, int TotalItems) = await mealRepository.GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyList<FavoriteMeal> favorites = await favoriteMealRepository.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        var favoriteItems = favorites
            .Take(favoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();

        MealId[] mealIds = [.. Items
            .Select(meal => meal.Id)
            .Distinct()];
        IReadOnlyDictionary<MealId, FavoriteMeal> favoritesByMealId = await favoriteMealRepository.GetByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);

        int totalPages = (int)Math.Ceiling(TotalItems / (double)sanitizedLimit);
        var allConsumptions = new PagedResponse<ConsumptionModel>(
            Items
                .Select(meal => {
                    FavoriteMeal? favorite = favoritesByMealId.GetValueOrDefault(meal.Id);
                    return meal.ToModel(
                        isFavorite: favorite is not null,
                        favoriteMealId: favorite?.Id.Value);
                })
                .ToList(),
            sanitizedPage,
            sanitizedLimit,
            totalPages,
            TotalItems);

        return Result.Success(new ConsumptionOverviewModel(allConsumptions, favoriteItems, favorites.Count));
    }

    private static MealQueryFilters CreateFilters(
        GetConsumptionsOverviewQuery request,
        DateTime? normalizedFrom,
        DateTime? normalizedTo) =>
        new(
            normalizedFrom,
            normalizedTo,
            ParseMealTypes(request.MealTypes),
            request.CaloriesFrom,
            request.CaloriesTo,
            request.HasImage,
            request.HasAiSession);

    private static MealType[]? ParseMealTypes(IReadOnlyCollection<string>? values) =>
        EnumFilterParser.ParseMany<MealType>(values);
}
