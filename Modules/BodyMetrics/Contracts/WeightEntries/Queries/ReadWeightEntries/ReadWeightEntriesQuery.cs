using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;

public sealed record ReadWeightEntriesQuery(
    UserId UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending) : IRequest<IReadOnlyList<WeightEntryModel>>;
