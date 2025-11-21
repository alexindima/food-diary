using System;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Cycles;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public record UpsertCycleDayCommand(
    UserId? UserId,
    CycleId CycleId,
    DateTime Date,
    bool IsPeriod,
    DailySymptomsDto Symptoms,
    string? Notes
) : ICommand<Result<CycleDayResponse>>;
