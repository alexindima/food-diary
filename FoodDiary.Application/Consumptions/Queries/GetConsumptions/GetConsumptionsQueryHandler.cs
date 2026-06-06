using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public class GetConsumptionsQueryHandler(
    IMealRepository mealRepository,
    IUserRepository userRepository,
    IFavoriteMealRepository favoriteMealRepository)
    : IQueryHandler<GetConsumptionsQuery, Result<PagedResponse<ConsumptionModel>>> {
    public async Task<Result<PagedResponse<ConsumptionModel>>> Handle(GetConsumptionsQuery request, CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<ConsumptionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(request.UserId!.Value);
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<ConsumptionModel>>(accessError);
        }

        int sanitizedPage = Math.Max(request.Page, 1);
        int sanitizedLimit = Math.Clamp(request.Limit, 1, 100);
        DateTime? normalizedFrom = request.DateFrom.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom.Value)
            : null;
        DateTime? normalizedTo = request.DateTo.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo.Value)
            : null;

        (IReadOnlyList<Meal> Items, int TotalItems) pageData = await mealRepository.GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            normalizedFrom,
            normalizedTo,
            cancellationToken).ConfigureAwait(false);

        MealId[] mealIds = [.. pageData.Items
            .Select(meal => meal.Id)
            .Distinct()];
        IReadOnlyDictionary<MealId, FavoriteMeal> favoritesByMealId = await favoriteMealRepository.GetByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)sanitizedLimit);
        var response = new PagedResponse<ConsumptionModel>(
            pageData.Items
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
            pageData.TotalItems);
        return Result.Success(response);
    }
}
