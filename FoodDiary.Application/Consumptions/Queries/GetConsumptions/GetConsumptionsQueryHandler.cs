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
using FoodDiary.Domain.Enums;

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

        MealId[] mealIds = [.. Items
            .Select(meal => meal.Id)
            .Distinct()];
        IReadOnlyDictionary<MealId, FavoriteMeal> favoritesByMealId = await favoriteMealRepository.GetByMealIdsAsync(userId, mealIds, cancellationToken).ConfigureAwait(false);
        int totalPages = (int)Math.Ceiling(TotalItems / (double)sanitizedLimit);
        var response = new PagedResponse<ConsumptionModel>(
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
        return Result.Success(response);
    }

    private static MealQueryFilters CreateFilters(GetConsumptionsQuery request, DateTime? normalizedFrom, DateTime? normalizedTo) =>
        new(
            normalizedFrom,
            normalizedTo,
            ParseMealTypes(request.MealTypes),
            request.CaloriesFrom,
            request.CaloriesTo,
            request.HasImage,
            request.HasAiSession);

    private static MealType[]? ParseMealTypes(IReadOnlyCollection<string>? values) {
        MealType[] parsed = [.. values?
            .Select(value => Enum.TryParse(value, ignoreCase: true, out MealType mealType) ? mealType : (MealType?)null)
            .OfType<MealType>()
            .Distinct() ?? []];

        return parsed.Length > 0 ? parsed : null;
    }
}
