using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public record GetHydrationEntriesQuery(
    UserId? UserId,
    DateTime DateUtc) : IQuery<Result<IReadOnlyList<HydrationEntryModel>>>;
