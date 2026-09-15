using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Hydration.Application.Commands.DeleteHydrationEntry;

public record DeleteHydrationEntryCommand(
    Guid? UserId,
    Guid HydrationEntryId) : ICommand<Result>, IUserRequest;
