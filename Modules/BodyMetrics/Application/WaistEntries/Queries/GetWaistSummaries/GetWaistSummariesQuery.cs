using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WaistEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Queries.GetWaistSummaries;

public record GetWaistSummariesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<WaistEntrySummaryModel>>>, IUserRequest;
