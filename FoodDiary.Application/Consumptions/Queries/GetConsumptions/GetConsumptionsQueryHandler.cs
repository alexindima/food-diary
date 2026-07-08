using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public sealed class GetConsumptionsQueryHandler(
    IConsumptionReadService consumptionReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetConsumptionsQuery, Result<PagedResponse<ConsumptionModel>>> {
    public async Task<Result<PagedResponse<ConsumptionModel>>> Handle(GetConsumptionsQuery request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<PagedResponse<ConsumptionModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        int sanitizedPage = Math.Max(request.Page, 1);
        int sanitizedLimit = Math.Clamp(request.Limit, 1, 100);
        DateTime? normalizedFrom = request.DateFrom.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateFrom.Value)
            : null;
        DateTime? normalizedTo = request.DateTo.HasValue
            ? UtcDateNormalizer.NormalizeInstantPreservingUnspecifiedAsUtc(request.DateTo.Value)
            : null;
        MealQueryFilters filters = CreateFilters(request, normalizedFrom, normalizedTo);

        PagedResponse<ConsumptionModel> response = await consumptionReadService.GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            filters,
            cancellationToken).ConfigureAwait(false);

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

    private static MealType[]? ParseMealTypes(IReadOnlyCollection<string>? values) =>
        EnumFilterParser.ParseMany<MealType>(values);
}
