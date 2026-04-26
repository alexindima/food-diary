using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.WaistEntries.Models;

namespace FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;

public record CreateWaistEntryCommand(
    Guid? UserId,
    DateTime Date,
    double Circumference
) : ICommand<Result<WaistEntryModel>>, IUserRequest;
