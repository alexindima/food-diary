using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Hydration.Queries.GetHydrationEntries;

public record GetHydrationEntriesQuery(
    UserId? UserId,
    DateTime DateUtc) : IQuery<Result<IReadOnlyList<HydrationEntryResponse>>>;
