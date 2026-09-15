using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Modules.Cycles.Application.Commands.ClearCycleDay;

public record ClearCycleDayCommand(
    Guid? UserId,
    Guid CycleProfileId,
    DateOnly Date
) : ICommand<Result>, IUserRequest;
