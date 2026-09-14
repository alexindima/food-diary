using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistHistoryPageSummary;

public sealed record GetWaistHistoryPageSummaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays,
    int EntriesLimit) : IQuery<Result<WaistHistoryPageSummaryModel>>, IUserRequest;
