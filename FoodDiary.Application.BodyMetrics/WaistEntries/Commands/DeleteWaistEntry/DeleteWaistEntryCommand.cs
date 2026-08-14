using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Commands.DeleteWaistEntry;

public record DeleteWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId
) : ICommand<Result>, IUserRequest;
