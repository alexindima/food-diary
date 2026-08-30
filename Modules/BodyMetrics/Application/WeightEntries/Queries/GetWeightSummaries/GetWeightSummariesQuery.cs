using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Queries.GetWeightSummaries;

public record GetWeightSummariesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<WeightEntrySummaryModel>>>, IUserRequest;
