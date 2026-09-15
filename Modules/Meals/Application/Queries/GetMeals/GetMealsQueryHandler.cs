using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Modules.Meals.Application.Common;
using FoodDiary.Modules.Meals.Service.Contracts.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Service.Contracts.Queries.GetMeals;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Application.Common.Time;
using FoodDiary.Modules.Meals.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Users.Common;

namespace FoodDiary.Modules.Meals.Application.Queries.GetMeals;

public sealed class GetMealsQueryHandler(
    IMealProjectionReadRepository mealRepository, ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMealsQuery, Result<PagedResponse<MealModel>>> {
    public async Task<Result<PagedResponse<MealModel>>> Handle(GetMealsQuery request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<MealModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int sanitizedPage = PaginationPolicy.NormalizePage(request.Page);
        int sanitizedLimit = PaginationPolicy.NormalizePageSize(request.Limit, defaultPageSize: 1);
        DateTime? normalizedFrom = request.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom.Value)
            : null;
        DateTime? normalizedTo = request.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo.Value)
            : null;
        MealQueryFilters filters = CreateFilters(request, normalizedFrom, normalizedTo);

        PagedResponse<MealModel> response = await GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            filters,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(response);
    }

    private static MealQueryFilters CreateFilters(GetMealsQuery request, DateTime? normalizedFrom, DateTime? normalizedTo) =>
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
    private async Task<PagedResponse<MealModel>> GetPagedAsync(
        UserId userId,
        int page,
        int limit,
        MealQueryFilters filters,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealProjectionReadModel> items, int totalItems) = await mealRepository.GetPagedMealProjectionsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await MealReadSupport.GetFavoritesByMealIdAsync(sender,
            userId,
            items,
            cancellationToken).ConfigureAwait(false);

        return MealReadSupport.ToPagedResponse(items, favoritesByMealId, page, limit, totalItems);
    }
}
