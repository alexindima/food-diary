using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public record CreateCycleCommand(
    UserId? UserId,
    DateTime StartDate,
    int? AverageLength,
    int? LutealLength,
    string? Notes
) : ICommand<Result<CycleModel>>;
