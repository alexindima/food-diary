using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.BodyMetrics.WaistEntries.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistHistoryPageSummary;

public sealed record GetWaistHistoryPageSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    int EntriesLimit) : IQuery<Result<WaistHistoryPageSummaryModel>>, IUserRequest;
