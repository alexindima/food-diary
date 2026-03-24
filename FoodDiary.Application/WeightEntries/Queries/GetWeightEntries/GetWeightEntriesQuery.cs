using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public record GetWeightEntriesQuery(
    Guid? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending
) : IQuery<Result<IReadOnlyList<WeightEntryModel>>>;
