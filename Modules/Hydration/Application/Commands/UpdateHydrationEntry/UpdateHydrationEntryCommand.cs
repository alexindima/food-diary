using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Hydration.Contracts.Models;

namespace FoodDiary.Modules.Hydration.Application.Commands.UpdateHydrationEntry;

public record UpdateHydrationEntryCommand(
    Guid? UserId,
    Guid HydrationEntryId,
    DateTime? TimestampUtc,
    int? AmountMl) : ICommand<Result<HydrationEntryModel>>, IUserRequest;
