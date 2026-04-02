using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Common.Time;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public class GetConsumptionsQueryHandler(IMealRepository mealRepository)
    : IQueryHandler<GetConsumptionsQuery, Result<PagedResponse<ConsumptionModel>>> {
    public async Task<Result<PagedResponse<ConsumptionModel>>> Handle(GetConsumptionsQuery request, CancellationToken cancellationToken) {
        if (request.UserId is null || request.UserId == Guid.Empty) {
            return Result.Failure<PagedResponse<ConsumptionModel>>(Errors.Authentication.InvalidToken);
        }

        var userId = new UserId(request.UserId!.Value);

        var sanitizedPage = Math.Max(request.Page, 1);
        var sanitizedLimit = Math.Clamp(request.Limit, 1, 100);
        var normalizedFrom = request.DateFrom.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeDateUsingLocalFallback(request.DateFrom.Value)
            : null;
        var normalizedTo = request.DateTo.HasValue
            ? (DateTime?)UtcDateNormalizer.NormalizeDateUsingLocalFallback(request.DateTo.Value)
            : null;

        var pageData = await mealRepository.GetPagedAsync(
            userId,
            sanitizedPage,
            sanitizedLimit,
            normalizedFrom,
            normalizedTo,
            cancellationToken);

        var response = pageData.ToPagedResponse(sanitizedPage, sanitizedLimit);
        return Result.Success(response);
    }
}
