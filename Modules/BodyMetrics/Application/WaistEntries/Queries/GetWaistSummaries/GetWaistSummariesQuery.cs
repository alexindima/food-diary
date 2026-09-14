using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Queries.GetWaistSummaries;

public record GetWaistSummariesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<WaistEntrySummaryModel>>>, IUserRequest;
