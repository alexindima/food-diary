using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.BodyMetrics.WeightEntries.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.BodyMetrics.WeightEntries.Queries.GetWeightHistoryPageSummary;

public sealed record GetWeightHistoryPageSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    int EntriesLimit) : IQuery<Result<WeightHistoryPageSummaryModel>>, IUserRequest;
