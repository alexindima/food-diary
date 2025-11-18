using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public record DeleteWaistEntryCommand(
    UserId? UserId,
    WaistEntryId WaistEntryId
) : ICommand<Result<bool>>;
