using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Fasting.Models;

namespace FoodDiary.Application.Fasting.Commands.UpdateCurrentFastingCheckIn;

public record UpdateCurrentFastingCheckInCommand(
    Guid? UserId,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string>? Symptoms,
    string? CheckInNotes) : ICommand<Result<FastingSessionModel>>, IUserRequest;
