using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Hydration.Models;

namespace FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;

public record UpdateHydrationEntryCommand(
    Guid? UserId,
    Guid HydrationEntryId,
    DateTime? TimestampUtc,
    int? AmountMl) : ICommand<Result<HydrationEntryModel>>, IUserRequest;
