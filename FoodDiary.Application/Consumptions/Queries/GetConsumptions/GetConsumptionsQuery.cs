using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Consumptions.Queries.GetConsumptions;

public record GetConsumptionsQuery(
    UserId? UserId,
    int Page,
    int Limit,
    DateTime? DateFrom,
    DateTime? DateTo) : IQuery<Result<PagedResponse<ConsumptionResponse>>>;
