using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadLatestWaistEntry;

public sealed record ReadLatestWaistEntryQuery(
    UserId UserId) : IRequest<WaistEntryModel?>;
