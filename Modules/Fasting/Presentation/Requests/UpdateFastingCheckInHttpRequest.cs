namespace FoodDiary.Modules.Fasting.Presentation.Requests;

public sealed record UpdateFastingCheckInHttpRequest(
    int HungerLevel,
    int EnergyLevel,
    int MoodLevel,
    IReadOnlyList<string>? Symptoms,
    string? CheckInNotes);
