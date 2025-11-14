using System;
using System.Collections.Generic;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Statistics;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public record GetStatisticsQuery(
    UserId? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<AggregatedStatisticsResponse>>>;
