using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;

public record DeleteHydrationEntryCommand(
    Guid? UserId,
    Guid HydrationEntryId) : ICommand<Result>, IUserRequest;
