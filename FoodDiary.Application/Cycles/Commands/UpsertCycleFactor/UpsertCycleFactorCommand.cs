using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.UpsertCycleFactor;

public record UpsertCycleFactorCommand(
    Guid? UserId,
    Guid CycleProfileId,
    int Type,
    DateTime StartDate,
    DateTime? EndDate,
    string? Notes,
    bool ClearNotes
) : ICommand<Result<CycleModel>>, IUserRequest;
