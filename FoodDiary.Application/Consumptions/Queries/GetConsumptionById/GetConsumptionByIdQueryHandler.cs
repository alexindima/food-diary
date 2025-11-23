using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptionById;

public class GetConsumptionByIdQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetConsumptionByIdQuery, Result<ConsumptionResponse>>
{
    public async Task<Result<ConsumptionResponse>> Handle(GetConsumptionByIdQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId is null || request.UserId == UserId.Empty)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Authentication.InvalidToken);
        }

        var meal = await mealRepository.GetByIdAsync(
            request.ConsumptionId,
            request.UserId.Value,
            includeItems: true,
            cancellationToken: cancellationToken);

        if (meal is null)
        {
            return Result.Failure<ConsumptionResponse>(Errors.Consumption.NotFound(request.ConsumptionId.Value));
        }

        return Result.Success(meal.ToResponse());
    }
}
