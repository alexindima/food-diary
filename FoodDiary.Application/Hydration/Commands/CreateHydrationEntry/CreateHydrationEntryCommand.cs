using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Models;

namespace FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;

public record CreateHydrationEntryCommand(
    Guid? UserId,
    DateTime TimestampUtc,
    int AmountMl) : ICommand<Result<HydrationEntryModel>>;
