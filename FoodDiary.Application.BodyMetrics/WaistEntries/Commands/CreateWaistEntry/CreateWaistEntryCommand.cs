using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.WaistEntries.Models;

namespace FoodDiary.Application.BodyMetrics.WaistEntries.Commands.CreateWaistEntry;

public record CreateWaistEntryCommand(
    Guid? UserId,
    DateTime Date,
    double CircumferenceCm
) : ICommand<Result<WaistEntryModel>>, IUserRequest;
