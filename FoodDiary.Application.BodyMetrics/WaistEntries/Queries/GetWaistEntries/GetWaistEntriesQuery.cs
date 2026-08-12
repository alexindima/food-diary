using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;

public record GetWaistEntriesQuery(
    Guid? UserId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? Limit,
    bool Descending
) : IQuery<Result<IReadOnlyList<WaistEntryModel>>>, IUserRequest;
