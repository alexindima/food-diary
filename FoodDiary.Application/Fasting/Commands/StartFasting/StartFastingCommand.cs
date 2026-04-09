using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public record StartFastingCommand(
    Guid? UserId,
    string? Protocol,
    string? PlanType,
    int? PlannedDurationHours,
    int? CyclicFastDays,
    int? CyclicEatDays,
    int? CyclicEatDayFastHours,
    int? CyclicEatDayEatingWindowHours,
    string? Notes) : ICommand<Result<FastingSessionModel>>, IUserRequest;
