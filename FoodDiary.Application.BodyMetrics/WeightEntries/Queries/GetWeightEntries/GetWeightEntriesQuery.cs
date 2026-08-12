using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WeightEntries.Models;

namespace FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;

public record GetWeightEntriesQuery(
    Guid? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending
) : IQuery<Result<IReadOnlyList<WeightEntryModel>>>, IUserRequest;
