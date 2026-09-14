using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetWeightHistoryPageSummary;

public sealed record GetWeightHistoryPageSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    int EntriesLimit) : IQuery<Result<WeightHistoryPageSummaryModel>>, IUserRequest;
