using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public class GetConsumptionsQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetConsumptionsQuery, Result<PagedResponse<ConsumptionModel>>> {
    public async Task<Result<PagedResponse<ConsumptionModel>>> Handle(GetConsumptionsQuery request, CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == UserId.Empty) {
            return Result.Failure<PagedResponse<ConsumptionModel>>(Errors.Authentication.InvalidToken);
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
