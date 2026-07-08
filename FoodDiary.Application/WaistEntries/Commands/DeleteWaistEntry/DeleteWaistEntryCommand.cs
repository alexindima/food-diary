using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public record DeleteWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId
) : ICommand<Result>, IUserRequest;
