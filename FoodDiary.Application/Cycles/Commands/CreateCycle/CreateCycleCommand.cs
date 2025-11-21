using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public record CreateCycleCommand(
    UserId? UserId,
    DateTime StartDate,
    int? AverageLength,
    int? LutealLength,
    string? Notes
) : ICommand<Result<CycleResponse>>;
