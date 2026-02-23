namespace FoodDiary.Domain.ValueObjects;

public sealed class DailySymptoms : IEquatable<DailySymptoms> {
    public int Pain { get; private set; }
    public int Mood { get; private set; }
    public int Edema { get; private set; }
    public int Headache { get; private set; }
    public int Energy { get; private set; }
    public int SleepQuality { get; private set; }
    public int Libido { get; private set; }

    private DailySymptoms() {
    }

    private DailySymptoms(int pain, int mood, int edema, int headache, int energy, int sleepQuality, int libido) {
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
        int libido) {
        return new DailySymptoms(
            EnsureInRange(pain, nameof(pain)),
            EnsureInRange(mood, nameof(mood)),
            EnsureInRange(edema, nameof(edema)),
            EnsureInRange(headache, nameof(headache)),
            EnsureInRange(energy, nameof(energy)),
            EnsureInRange(sleepQuality, nameof(sleepQuality)),
            EnsureInRange(libido, nameof(libido)));
    }

    public DailySymptoms Update(
        int? pain = null,
        int? mood = null,
        int? edema = null,
        int? headache = null,
        int? energy = null,
        int? sleepQuality = null,
        int? libido = null) {
        return Create(
            pain ?? Pain,
            mood ?? Mood,
            edema ?? Edema,
            headache ?? Headache,
            energy ?? Energy,
            sleepQuality ?? SleepQuality,
            libido ?? Libido);
    }

    public bool Equals(DailySymptoms? other) {
        if (other is null) {
            return false;
        }

        if (ReferenceEquals(this, other)) {
            return true;
        }

        return Pain == other.Pain
               && Mood == other.Mood
               && Edema == other.Edema
               && Headache == other.Headache
               && Energy == other.Energy
               && SleepQuality == other.SleepQuality
               && Libido == other.Libido;
    }

    public override bool Equals(object? obj) {
        return obj is DailySymptoms other && Equals(other);
    }

    // ReSharper disable NonReadonlyMemberInGetHashCode
    public override int GetHashCode() {
        return HashCode.Combine(Pain, Mood, Edema, Headache, Energy, SleepQuality, Libido);
    }

    private static int EnsureInRange(int value, string paramName) {
        return value is < 0 or > 9
            ? throw new ArgumentOutOfRangeException(paramName, "Value must be in range [0, 9].")
            : value;
    }
}
