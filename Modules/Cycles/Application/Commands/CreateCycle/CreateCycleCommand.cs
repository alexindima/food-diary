using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Cycles.Models;

namespace FoodDiary.Application.Cycles.Commands.CreateCycle;

public record CreateCycleCommand(
    Guid? UserId,
    DateOnly TrackingStartDate,
    int Mode,
    int? AverageCycleLength,
    int? AveragePeriodLength,
    int? LutealLength,
    bool IsRegular,
    bool IsOnboardingComplete,
    bool ShowFertilityEstimates,
    bool DiscreetNotifications,
    string? Notes,
    int? Goal = null,
    int? ReproductiveState = null,
    bool HideFromDashboard = false,
    bool? CycleTrackingConsentGranted = null,
    bool NutritionInsightsConsentGranted = false,
    bool FertilitySignalsConsentGranted = false
) : ICommand<Result<CycleModel>>, IUserRequest;
