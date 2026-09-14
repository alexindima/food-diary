using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightSummaries;

public sealed record ReadWeightSummariesQuery(
    UserId UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int QuantizationDays) : IRequest<IReadOnlyList<WeightEntrySummaryModel>>;
