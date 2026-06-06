using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;

public record DeleteWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId
) : ICommand<Result>, IUserRequest;
