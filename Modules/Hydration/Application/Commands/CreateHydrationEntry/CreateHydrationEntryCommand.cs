using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Contracts.Models;

namespace FoodDiary.Modules.Hydration.Application.Commands.CreateHydrationEntry;

public record CreateHydrationEntryCommand(
    Guid? UserId,
    DateTime TimestampUtc,
    int AmountMl) : ICommand<Result<HydrationEntryModel>>, IUserRequest;
