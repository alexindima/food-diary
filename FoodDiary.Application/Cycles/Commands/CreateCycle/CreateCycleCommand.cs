using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public record CreateCycleCommand(
    Guid? UserId,
    DateTime StartDate,
    int? AverageLength,
    int? LutealLength,
    string? Notes
) : ICommand<Result<CycleModel>>, IUserRequest;
