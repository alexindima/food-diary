using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public record UpsertCycleDayCommand(
    Guid? UserId,
    CycleId CycleId,
    DateTime Date,
    bool IsPeriod,
    DailySymptomsModel Symptoms,
    string? Notes
) : ICommand<Result<CycleDayModel>>;
