using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Application.Meals.Common.Time;
using FoodDiary.Application.Meals.Common.Validation;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Meals.Queries.GetMeals;

public sealed class GetMealsQueryHandler(
    IMealReadService mealReadService,
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

        PagedResponse<MealModel> response = await mealReadService.GetPagedAsync(
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
}
