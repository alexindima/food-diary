using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Commands.UpdateCycleSettings;

public sealed record UpdateCycleSettingsCommand(
    Guid? UserId,
    Guid CycleProfileId,
    int Mode,
    int AverageCycleLength,
    int AveragePeriodLength,
    int LutealLength,
    bool IsRegular,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications,
    int? Goal = null,
    int? ReproductiveState = null,
    bool? HideFromDashboard = null) : ICommand<Result<CycleModel>>, IUserRequest;
