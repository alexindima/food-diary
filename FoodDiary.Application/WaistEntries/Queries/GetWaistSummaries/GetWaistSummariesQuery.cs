using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;

public record GetWaistSummariesQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays
) : IQuery<Result<IReadOnlyList<WaistEntrySummaryModel>>>, IUserRequest;
