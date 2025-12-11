using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public record UpdateHydrationEntryCommand(
    UserId? UserId,
    HydrationEntryId HydrationEntryId,
    DateTime? TimestampUtc,
    int? AmountMl) : ICommand<Result<HydrationEntryResponse>>;
