using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Statistics.Models;

namespace FoodDiary.Application.Statistics.Queries.GetStatistics;

public record GetStatisticsQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<AggregatedStatisticsModel>>>;
