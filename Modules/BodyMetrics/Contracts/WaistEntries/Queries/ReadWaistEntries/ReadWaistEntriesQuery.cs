using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;

public sealed record ReadWaistEntriesQuery(
    UserId UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending) : IRequest<IReadOnlyList<WaistEntryModel>>;
