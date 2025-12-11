using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public record CreateHydrationEntryCommand(
    UserId? UserId,
    DateTime TimestampUtc,
    int AmountMl) : ICommand<Result<HydrationEntryResponse>>;
