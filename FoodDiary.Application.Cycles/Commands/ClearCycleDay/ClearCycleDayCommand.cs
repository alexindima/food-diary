using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Cycles.Commands.ClearCycleDay;

public record ClearCycleDayCommand(
    Guid? UserId,
    Guid CycleProfileId,
    DateOnly Date
) : ICommand<Result>, IUserRequest;
