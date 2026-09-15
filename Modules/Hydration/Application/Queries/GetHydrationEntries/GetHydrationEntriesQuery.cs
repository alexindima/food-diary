using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Contracts.Models;

namespace FoodDiary.Modules.Hydration.Application.Queries.GetHydrationEntries;

public record GetHydrationEntriesQuery(
    Guid? UserId,
    DateTime DateUtc) : IQuery<Result<IReadOnlyList<HydrationEntryModel>>>, IUserRequest;
