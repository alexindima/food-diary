using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProfileBirthDateMeaningTests {
    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local)]
    public void Decoding_PreservesOriginalTicksKindAndMutationNormalization(DateTimeKind kind) {
        DateTime encoded = new DateTime(2000, 6, 1, 23, 14, 16, kind).AddTicks(1234567);
        var meaning = ProfileBirthDate.FromEncodedDateTime(encoded);
        Assert.Multiple(
            () => Assert.Equal(encoded.Ticks, meaning.EncodedDateTime.Ticks),
            () => Assert.Equal(kind, meaning.EncodedDateTime.Kind),
            () => Assert.Equal(new DateOnly(2000, 6, 1), meaning.CalendarDay));

        var user = User.Create("birth-meaning@example.com", "hash");
        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Set(meaning)));
        DateTime expected = kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(encoded.Date, DateTimeKind.Utc)
            : DateTime.SpecifyKind(encoded.ToUniversalTime().Date, DateTimeKind.Utc);
        Assert.Equal(expected, user.BirthDate);
        Assert.Equal(DateTimeKind.Utc, user.BirthDate!.Value.Kind);
    }

    [Fact]
    public void FieldChanges_DistinguishOmissionClearingAndSetWithoutChangingAuditNoOps() {
        var user = User.Create("birth-field-change@example.com", "hash");
        var original = new DateTime(2000, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        user.UpdatePersonalInfo(birthDate: original);
        DateTime? audit = user.NutritionProfile.ModifiedOnUtc;
        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Unchanged<ProfileBirthDate>()));
        Assert.Equal(original, user.BirthDate);
        Assert.Equal(audit, user.NutritionProfile.ModifiedOnUtc);
        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Clear<ProfileBirthDate>()));
        Assert.Null(user.BirthDate);
    }

    [Fact]
    public void FutureDate_StillFailsAtTheOwnerBeforeGenderAndWithoutPartialUpdates() {
        using IDisposable scope = DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)));
        var user = User.Create("birth-future@example.com", "hash");
        var future = ProfileBirthDate.FromEncodedDateTime(new DateTime(2026, 6, 2, 0, 0, 0, DateTimeKind.Unspecified));
        ArgumentOutOfRangeException error = Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdatePersonalInfoChanges(
            Changes(FieldChanges.Set(future)) with { FirstName = "Must not persist", Gender = "invalid" }));
        Assert.Multiple(
            () => Assert.Equal("birthDate", error.ParamName),
            () => Assert.Null(user.BirthDate),
            () => Assert.Null(user.FirstName),
            () => Assert.Null(user.ModifiedOnUtc),
            () => Assert.Null(user.NutritionProfile.ModifiedOnUtc));
    }

    [Fact]
    public void TimeLaterToday_IsAllowedAfterTheExistingDateNormalization() {
        using IDisposable scope = DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 6, 1, 1, 0, 0, TimeSpan.Zero)));
        var user = User.Create("birth-today@example.com", "hash");
        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Set(ProfileBirthDate.FromEncodedDateTime(
            new DateTime(2026, 6, 1, 23, 59, 0, DateTimeKind.Utc)))));
        Assert.Equal(new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc), user.BirthDate);
    }

    [Theory]
    [InlineData(5, 31, 1805)]
    [InlineData(6, 1, 1800)]
    [InlineData(6, 2, 1800)]
    public void Calculator_PreservesBirthdayBoundaryAndIgnoresEncodedTime(int month, int day, double expected) {
        using IDisposable scope = DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, month, day, 12, 0, 0, TimeSpan.Zero)));
        var encoded = new DateTime(2000, 6, 1, 23, 59, 0, DateTimeKind.Unspecified);
        var input = BmrCalculationInput.FromMeasurements(ProfileWeightKg.Create(80), ProfileHeightCm.Create(180),
            ProfileBirthDate.FromEncodedDateTime(encoded), "M");
        Assert.Equal(expected, User.CalculateBmrFromProfile(input));
    }

    [Theory]
    [InlineData(27, 1805)]
    [InlineData(28, 1800)]
    public void Calculator_PreservesLeapBirthdayAddYearsPolicy(int day, double expected) {
        using IDisposable scope = DomainTime.Override(new FixedTimeProvider(new DateTimeOffset(2026, 2, day, 12, 0, 0, TimeSpan.Zero)));
        var input = BmrCalculationInput.FromMeasurements(ProfileWeightKg.Create(80), ProfileHeightCm.Create(180),
            ProfileBirthDate.FromEncodedDateTime(new DateTime(2000, 2, 29)), "M");
        Assert.Equal(expected, User.CalculateBmrFromProfile(input));
    }

    private static UserPersonalInfoChanges Changes(FieldChange<ProfileBirthDate> date) =>
        new(Username: null, FirstName: null, LastName: null, date, Gender: null, WeightKg: null, HeightCm: null);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
