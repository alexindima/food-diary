using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.UpdateWaistEntry;

public record UpdateWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId,
    DateTime Date,
    double CircumferenceCm
) : ICommand<Result<WaistEntryModel>>, IUserRequest;
