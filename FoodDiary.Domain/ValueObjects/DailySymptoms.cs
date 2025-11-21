using System;

namespace FoodDiary.Domain.ValueObjects;

/// <summary>
/// Captures daily symptom scores on a 0-9 scale.
/// </summary>
public sealed class DailySymptoms
{
    public int Pain { get; private set; }
    public int Mood { get; private set; }
    public int Edema { get; private set; }
    public int Headache { get; private set; }
    public int Energy { get; private set; }
    public int SleepQuality { get; private set; }
    public int Libido { get; private set; }

    private DailySymptoms()
    {
    }

    private DailySymptoms(int pain, int mood, int edema, int headache, int energy, int sleepQuality, int libido)
    {
        Pain = pain;
        Mood = mood;
        Edema = edema;
        Headache = headache;
        Energy = energy;
        SleepQuality = sleepQuality;
        Libido = libido;
    }

    public static DailySymptoms Create(
        int pain,
        int mood,
        int edema,
        int headache,
        int energy,
        int sleepQuality,
        int libido)
    {
        return new DailySymptoms(
            Clamp(pain),
            Clamp(mood),
            Clamp(edema),
            Clamp(headache),
            Clamp(energy),
            Clamp(sleepQuality),
            Clamp(libido));
    }

    public DailySymptoms Update(
        int? pain = null,
        int? mood = null,
        int? edema = null,
        int? headache = null,
        int? energy = null,
        int? sleepQuality = null,
        int? libido = null)
    {
        return Create(
            pain ?? Pain,
            mood ?? Mood,
            edema ?? Edema,
            headache ?? Headache,
            energy ?? Energy,
            sleepQuality ?? SleepQuality,
            libido ?? Libido);
    }

    private static int Clamp(int value) => Math.Clamp(value, 0, 9);
}
