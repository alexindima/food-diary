using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoritesOverview;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Meals.Application.Common;
using FoodDiary.Modules.Meals.Application.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Meals.Domain.Contracts.Enums;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Meals.Application.Common.Time;
using FoodDiary.Modules.Meals.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Users.Contracts.Common;

namespace FoodDiary.Modules.Meals.Application.Queries.GetMealsOverview;

public sealed class GetMealsOverviewQueryHandler(
    IMealProjectionReadRepository mealRepository, ISender sender,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetMealsOverviewQuery, Result<MealOverviewModel>> {
    public async Task<Result<MealOverviewModel>> Handle(
        GetMealsOverviewQuery request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<MealOverviewModel>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int sanitizedPage = PaginationPolicy.NormalizePage(request.Page);
        int sanitizedLimit = PaginationPolicy.NormalizePageSize(request.Limit, defaultPageSize: 1);
        int favoriteLimit = Math.Clamp(request.FavoriteLimit, 0, 50);
        DateTime? normalizedFrom = request.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom.Value)
            : null;
        DateTime? normalizedTo = request.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo.Value)
            : null;
        MealQueryFilters filters = CreateFilters(request, normalizedFrom, normalizedTo);
        if (!LocalCalendar.TryResolve(request.TimeZoneId, request.TimeZoneOffsetMinutes, out TimeZoneInfo timeZone)) {
            return Result.Failure<MealOverviewModel>(FoodDiary.Application.Abstractions.Common.Abstractions.Results.Errors.Validation.Invalid(nameof(request.TimeZoneId), "Invalid time zone."));
        }

        MealOverviewModel overview = await GetOverviewAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            favoriteLimit,
            filters, timeZone, request.IncludeFavorites,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(overview);
    }

    private static MealQueryFilters CreateFilters(
        GetMealsOverviewQuery request,
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
    private async Task<MealOverviewModel> GetOverviewAsync(
        UserId userId,
        int page,
        int limit,
        int favoriteLimit,
        MealQueryFilters filters, TimeZoneInfo timeZone, bool includeFavorites,
        CancellationToken cancellationToken) {
        (IReadOnlyList<MealProjectionReadModel> items, int totalItems) = await mealRepository.GetPagedMealProjectionsAsync(
            userId,
            page,
            limit,
            filters,
            cancellationToken).ConfigureAwait(false);

        (IReadOnlyList<MealFavoriteMealModel> favoriteItems, int favoriteCount) =
            includeFavorites
                ? await sender.Send(new ReadMealFavoritesOverviewQuery(userId, favoriteLimit), cancellationToken).ConfigureAwait(false)
                : (Array.Empty<MealFavoriteMealModel>(), 0);
        IReadOnlyDictionary<MealId, FavoriteMealId> favoritesByMealId = await MealReadSupport.GetFavoritesByMealIdAsync(sender,
            userId,
            items,
            cancellationToken).ConfigureAwait(false);

        var allMeals = MealReadSupport.ToPagedResponse(items, favoritesByMealId, page, limit, totalItems);

        DateOnly[] dates = [.. items.Select(item => LocalCalendar.DateAt(item.Date, timeZone)).Distinct()];
        IReadOnlyList<FoodDiary.Modules.Meals.Application.Abstractions.Models.MealDaySummary> summaries = dates.Length == 0 ? [] : await mealRepository.GetDaySummariesAsync(userId, dates, timeZone, cancellationToken).ConfigureAwait(false);
        return new MealOverviewModel(allMeals, favoriteItems, favoriteCount) { DaySummaries = summaries };
    }
}
