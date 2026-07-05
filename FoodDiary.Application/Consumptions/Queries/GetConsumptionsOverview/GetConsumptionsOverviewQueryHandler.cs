using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionsOverview;

public sealed class GetConsumptionsOverviewQueryHandler(
    IConsumptionReadService consumptionReadService,
    ICurrentUserAccessService currentUserAccessService)
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

        ConsumptionOverviewModel overview = await consumptionReadService.GetOverviewAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            favoriteLimit,
            filters,
            cancellationToken).ConfigureAwait(false);

        return Result.Success(overview);
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
