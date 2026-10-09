namespace FoodDiary.Modules.Users.Domain.ValueObjects;

public sealed record BmrCalculationInput {
    internal double? WeightKg { get; }
    internal double? HeightCm { get; }
    internal ProfileBirthDate? BirthDate { get; }
    internal string? Gender { get; }

    private BmrCalculationInput(double? weightKg, double? heightCm, ProfileBirthDate? birthDate, string? gender) {
        WeightKg = weightKg;
        HeightCm = heightCm;
        BirthDate = birthDate;
        Gender = gender;
    }

    public static BmrCalculationInput FromMeasurements(
        ProfileWeightKg? weight, ProfileHeightCm? height, ProfileBirthDate? birthDate, string? gender) =>
        new(weight?.Value, height?.Value, birthDate, gender);

    // Historical profile projections retain the calculator's existing missing/invalid-data policy.
    public static BmrCalculationInput FromStoredProfile(
        double? weightKg, double? heightCm, DateTime? birthDate, string? gender) =>
        new(weightKg, heightCm, birthDate is { } encoded ? ProfileBirthDate.FromEncodedDateTime(encoded) : null, gender);
}
