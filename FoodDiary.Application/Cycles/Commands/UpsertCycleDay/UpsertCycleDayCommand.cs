using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleDay;

public record UpsertCycleDayCommand(
    Guid? UserId,
    Guid CycleProfileId,
    DateTime Date,
    BleedingLogCommandModel? Bleeding,
    IReadOnlyCollection<SymptomLogCommandModel> Symptoms,
    FertilitySignalCommandModel? FertilitySignal
) : ICommand<Result<CycleLogDayModel>>, IUserRequest;
