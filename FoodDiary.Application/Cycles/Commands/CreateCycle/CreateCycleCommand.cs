using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public record CreateCycleCommand(
    Guid? UserId,
    DateTime TrackingStartDate,
    int Mode,
    int? AverageCycleLength,
    int? AveragePeriodLength,
    int? LutealLength,
    bool IsRegular,
    bool IsOnboardingComplete,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications,
    string? Notes
) : ICommand<Result<CycleModel>>, IUserRequest;
