using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Models;
using FoodDiary.Application.Consumptions.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public record GetConsumptionsQuery(
    UserId? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo) : IQuery<Result<PagedResponse<ConsumptionModel>>>;
