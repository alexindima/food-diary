using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Statistics.Application.Models;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetStatistics;

public record GetStatisticsQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<AggregatedStatisticsModel>>>, IUserRequest;
