using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Meals.Common.Time;
using FoodDiary.Application.Meals.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Meals.Queries.GetMealsOverview;

public sealed class GetMealsOverviewQueryHandler(
    IMealReadService mealReadService,
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

        MealOverviewModel overview = await mealReadService.GetOverviewAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            favoriteLimit,
            filters,
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
}
