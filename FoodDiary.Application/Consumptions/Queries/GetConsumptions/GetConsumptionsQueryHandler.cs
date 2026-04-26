using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

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
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<PagedResponse<ConsumptionModel>>(accessError);
        }

        var sanitizedPage = Math.Max(request.Page, 1);
        var sanitizedLimit = Math.Clamp(request.Limit, 1, 100);
        var normalizedFrom = request.DateFrom.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeDateUsingLocalFallback(request.DateFrom.Value)
            : null;
        var normalizedTo = request.DateTo.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeDateUsingLocalFallback(request.DateTo.Value)
            : null;

        var pageData = await mealRepository.GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var mealIds = pageData.Items
            .Select(meal => meal.Id)
            .Distinct()
            .ToArray();
        var favoritesByMealId = await favoriteMealRepository.GetByMealIdsAsync(userId, mealIds, cancellationToken);
        var totalPages = (int)Math.Ceiling(pageData.TotalItems / (double)sanitizedLimit);
        var response = new PagedResponse<ConsumptionModel>(
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
        return Result.Success(response);
    }
}
