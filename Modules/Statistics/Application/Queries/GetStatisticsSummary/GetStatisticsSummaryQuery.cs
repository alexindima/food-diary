using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;

public sealed record GetStatisticsSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    DateOnly? BodyDateFrom = null,
    DateOnly? BodyDateTo = null,
    string? TimeZoneId = null
) : IQuery<Result<StatisticsSummaryModel>>, IUserRequest;
