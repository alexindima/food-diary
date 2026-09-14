using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.GetWeightEntries;

public record GetWeightEntriesQuery(
    Guid? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending
) : IQuery<Result<IReadOnlyList<WeightEntryModel>>>, IUserRequest;
