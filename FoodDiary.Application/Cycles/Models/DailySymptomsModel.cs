namespace FoodDiary.Application.Cycles.Models;

public sealed record DailySymptomsModel(
    int Pain,
    int Mood,
    int Edema,
    int Headache,
    int Energy,
    int SleepQuality,
    int Libido);
