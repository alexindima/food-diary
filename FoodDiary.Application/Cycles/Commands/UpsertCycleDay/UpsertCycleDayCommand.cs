using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public record UpsertCycleDayCommand(
    Guid? UserId,
    Guid CycleId,
    DateTime Date,
    bool IsPeriod,
    DailySymptomsModel Symptoms,
    string? Notes,
    bool ClearNotes
) : ICommand<Result<CycleDayModel>>;
