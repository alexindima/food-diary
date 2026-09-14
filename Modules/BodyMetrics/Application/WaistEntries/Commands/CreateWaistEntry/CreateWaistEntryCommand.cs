using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;

namespace FoodDiary.Modules.BodyMetrics.Application.WaistEntries.Commands.CreateWaistEntry;

public record CreateWaistEntryCommand(
    Guid? UserId,
    DateTime Date,
    double CircumferenceCm
) : ICommand<Result<WaistEntryModel>>, IUserRequest;
