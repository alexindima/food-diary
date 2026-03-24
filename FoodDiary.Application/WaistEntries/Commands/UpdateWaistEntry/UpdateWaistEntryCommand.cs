using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;

public record UpdateWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId,
    DateTime Date,
    double Circumference
) : ICommand<Result<WaistEntryModel>>;
