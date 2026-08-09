using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Statistics.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Statistics.Queries.GetStatisticsSummary;

public sealed record GetStatisticsSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<StatisticsSummaryModel>>, IUserRequest;
