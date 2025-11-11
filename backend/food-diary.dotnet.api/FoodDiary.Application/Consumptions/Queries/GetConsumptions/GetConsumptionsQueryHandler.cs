using System;
using System.Threading;
using System.Threading.Tasks;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public class GetConsumptionsQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetConsumptionsQuery, Result<PagedResponse<ConsumptionResponse>>>
{
    public async Task<Result<PagedResponse<ConsumptionResponse>>> Handle(GetConsumptionsQuery request, CancellationToken cancellationToken)
    {
        if (request.UserId is null || request.UserId == UserId.Empty)
        {
            return Result.Failure<PagedResponse<ConsumptionResponse>>(Errors.Authentication.InvalidToken);
        }

        var sanitizedPage = Math.Max(request.Page, 1);
        var sanitizedLimit = Math.Clamp(request.Limit, 1, 100);

        var pageData = await mealRepository.GetPagedAsync(
            request.UserId.Value,
            sanitizedPage,
            sanitizedLimit,
            request.DateFrom,
            request.DateTo,
            cancellationToken);

        var response = pageData.ToPagedResponse(sanitizedPage, sanitizedLimit);
        return Result.Success(response);
    }
}
