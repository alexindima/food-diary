using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public record UpdateHydrationEntryCommand(
    UserId? UserId,
    HydrationEntryId HydrationEntryId,
    DateTime? TimestampUtc,
    int? AmountMl) : ICommand<Result<HydrationEntryModel>>;
