using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public record GetConsumptionsQuery(
    Guid? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo) : IQuery<Result<PagedResponse<ConsumptionModel>>>, IUserRequest;
