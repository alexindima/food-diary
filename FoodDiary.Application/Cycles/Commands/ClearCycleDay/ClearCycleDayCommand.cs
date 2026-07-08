using FoodDiary.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;

namespace FoodDiary.Application.Cycles.Commands.ClearCycleDay;

public record ClearCycleDayCommand(
    Guid? UserId,
    Guid CycleProfileId,
    DateTime Date
) : ICommand<Result>, IUserRequest;
