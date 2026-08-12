using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightHistoryPageSummary;

public sealed record GetWeightHistoryPageSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    int EntriesLimit) : IQuery<Result<WeightHistoryPageSummaryModel>>, IUserRequest;
