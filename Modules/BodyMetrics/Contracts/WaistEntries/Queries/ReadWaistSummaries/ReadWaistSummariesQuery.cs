using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistSummaries;

public sealed record ReadWaistSummariesQuery(
    UserId UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays) : IRequest<IReadOnlyList<WaistEntrySummaryModel>>;
