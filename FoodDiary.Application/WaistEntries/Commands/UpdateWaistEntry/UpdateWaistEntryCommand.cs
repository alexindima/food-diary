using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public record UpdateWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId,
    DateTime Date,
    double Circumference
) : ICommand<Result<WaistEntryModel>>, IUserRequest;
