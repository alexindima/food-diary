using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.StartFasting;

public record StartFastingCommand(
    Guid? UserId,
    string Protocol,
    int? PlannedDurationHours,
    string? Notes) : ICommand<Result<FastingSessionModel>>, IUserRequest;
