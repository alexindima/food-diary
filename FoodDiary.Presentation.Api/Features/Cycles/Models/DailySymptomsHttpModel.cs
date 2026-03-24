namespace FoodDiary.Presentation.Api.Features.Cycles.Models;

public sealed record DailySymptomsHttpModel(
    int Pain,
    int Mood,
    int Edema,
    int Headache,
    int Energy,
    int SleepQuality,
    int Libido);
