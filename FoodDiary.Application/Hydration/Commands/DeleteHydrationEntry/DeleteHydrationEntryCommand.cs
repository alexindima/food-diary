using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public record DeleteHydrationEntryCommand(
    Guid? UserId,
    Guid HydrationEntryId) : ICommand<Result<bool>>;
