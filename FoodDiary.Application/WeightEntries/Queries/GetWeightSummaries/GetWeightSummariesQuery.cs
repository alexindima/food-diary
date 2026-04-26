using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;

public record GetWeightSummariesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<WeightEntrySummaryModel>>>, IUserRequest;
