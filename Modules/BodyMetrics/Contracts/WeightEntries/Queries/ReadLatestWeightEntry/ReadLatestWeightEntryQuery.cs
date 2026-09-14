using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Mediator;

namespace FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadLatestWeightEntry;

public sealed record ReadLatestWeightEntryQuery(
    UserId UserId) : IRequest<WeightEntryModel?>;
