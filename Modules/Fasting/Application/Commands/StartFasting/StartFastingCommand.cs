using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.StartFasting;

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
