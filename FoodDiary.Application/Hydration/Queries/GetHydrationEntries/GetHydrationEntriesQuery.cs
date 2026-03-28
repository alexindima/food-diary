using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Models;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public record GetHydrationEntriesQuery(
    Guid? UserId,
    DateTime DateUtc) : IQuery<Result<IReadOnlyList<HydrationEntryModel>>>, IUserRequest;
