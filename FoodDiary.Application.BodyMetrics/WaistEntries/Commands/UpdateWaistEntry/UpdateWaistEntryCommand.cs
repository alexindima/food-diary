using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WaistEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Commands.UpdateWaistEntry;

public record UpdateWaistEntryCommand(
    Guid? UserId,
    Guid WaistEntryId,
    DateTime Date,
    double Circumference
) : ICommand<Result<WaistEntryModel>>, IUserRequest;
