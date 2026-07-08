using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public sealed class GetConsumptionByIdQueryHandler(
    IConsumptionReadService consumptionReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetConsumptionByIdQuery, Result<ConsumptionModel>> {
    public async Task<Result<ConsumptionModel>> Handle(GetConsumptionByIdQuery request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            request.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<ConsumptionModel>(userIdResult);
        }

        Result<MealId> consumptionIdResult = RequiredIdParser.Parse(
            request.ConsumptionId,
            nameof(request.ConsumptionId),
            "Consumption id must not be empty.",
            value => new MealId(value));
        if (consumptionIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<ConsumptionModel, MealId>(consumptionIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId consumptionId = consumptionIdResult.Value;

        ConsumptionModel? consumption = await consumptionReadService.GetByIdAsync(
            userId,
            consumptionId,
            cancellationToken).ConfigureAwait(false);

        return consumption is null
            ? Result.Failure<ConsumptionModel>(Errors.Consumption.NotFound(request.ConsumptionId))
            : Result.Success(consumption);
    }
}
