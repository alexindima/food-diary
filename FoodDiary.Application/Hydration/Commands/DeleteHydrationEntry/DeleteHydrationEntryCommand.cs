using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public record DeleteHydrationEntryCommand(
    UserId? UserId,
    HydrationEntryId HydrationEntryId) : ICommand<Result<bool>>;
