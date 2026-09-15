using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;

public sealed record GetStatisticsSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<StatisticsSummaryModel>>, IUserRequest;
