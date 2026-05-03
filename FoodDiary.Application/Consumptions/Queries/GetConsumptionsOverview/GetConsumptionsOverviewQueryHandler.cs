using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;

public sealed class GetConsumptionsOverviewQueryHandler(
    IMealRepository mealRepository,
    IUserRepository userRepository,
    IFavoriteMealRepository favoriteMealRepository)
    : IQueryHandler<GetConsumptionsOverviewQuery, Result<ConsumptionOverviewModel>> {
    public async Task<Result<ConsumptionOverviewModel>> Handle(
        GetConsumptionsOverviewQuery request,
        CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == Guid.Empty) {
            return Result.Failure<ConsumptionOverviewModel>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(request.UserId.Value);
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<ConsumptionOverviewModel>(accessError);
        }

        var sanitizedPage = Math.Max(request.Page, 1);
        var sanitizedLimit = Math.Clamp(request.Limit, 1, 100);
        var favoriteLimit = Math.Clamp(request.FavoriteLimit, 1, 50);
        var normalizedFrom = request.DateFrom.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom.Value)
            : null;
        var normalizedTo = request.DateTo.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo.Value)
            : null;

        var pageData = await mealRepository.GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var favorites = await favoriteMealRepository.GetAllAsync(userId, cancellationToken);
        var favoriteItems = favorites
            .Take(favoriteLimit)
            .Select(favorite => favorite.ToModel())
            .ToList();

        var mealIds = pageData.Items
            .Select(meal => meal.Id)
            .Distinct()
            .ToArray();
        var favoritesByMealId = await favoriteMealRepository.GetByMealIdsAsync(userId, mealIds, cancellationToken);

        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)sanitizedLimit);
        var allConsumptions = new PagedResponse<ConsumptionModel>(
            pageData.Items
                .Select(meal => {
                    var favorite = favoritesByMealId.GetValueOrDefault(meal.Id);
                    return meal.ToModel(
                        isFavorite: favorite is not null,
                        favoriteMealId: favorite?.Id.Value);
                })
                .ToList(),
            sanitizedPage,
            sanitizedLimit,
            totalPages,
            pageData.TotalItems);

        return Result.Success(new ConsumptionOverviewModel(allConsumptions, favoriteItems, favorites.Count));
    }
}
