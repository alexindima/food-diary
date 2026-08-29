using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Fasting.Contracts.Read.Models;

namespace FoodDiary.Modules.Fasting.Application.Commands.UpdateCurrentFastingCheckIn;

public record UpdateCurrentFastingCheckInCommand(
    Guid? UserId,
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string>? Symptoms,
    string? CheckInNotes) : ICommand<Result<FastingSessionModel>>, IUserRequest;
