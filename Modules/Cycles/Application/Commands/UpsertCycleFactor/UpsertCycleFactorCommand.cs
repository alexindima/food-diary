using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Cycles.Contracts.Models;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpsertCycleFactor;

public record UpsertCycleFactorCommand(
    Guid? UserId,
    Guid CycleProfileId,
    int Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Notes,
    bool ClearNotes
) : ICommand<Result<CycleModel>>, IUserRequest;
