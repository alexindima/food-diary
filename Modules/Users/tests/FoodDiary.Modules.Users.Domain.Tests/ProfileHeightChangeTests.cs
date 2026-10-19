using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class ProfileHeightChangeTests {
    [Fact]
    public void Clear_RemovesHeightAndTouchesOnlyNutritionProfileWithIdempotentRepeat() {
        DateTimeOffset initial = new(2026, 10, 19, 0, 0, 0, TimeSpan.Zero);
        using IDisposable initialClock = DomainTime.Override(new FixedTimeProvider(initial));
        var user = User.Create("height-clear@example.com", "hash");
        user.UpdatePersonalInfo(birthDate: new DateTime(2000, 1, 1), gender: "M", weight: 72, height: 170);
        Assert.NotNull(User.CalculateBmr(user.WeightKg, user.HeightCm, user.BirthDate, user.Gender));

        using IDisposable clearClock = DomainTime.Override(new FixedTimeProvider(initial.AddDays(1)));
        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Clear<ProfileHeightCm>()));

        Assert.Multiple(
            () => Assert.Null(user.HeightCm),
            () => Assert.Equal(72, user.WeightKg),
            () => Assert.Null(User.CalculateBmr(user.WeightKg, user.HeightCm, user.BirthDate, user.Gender)),
            () => Assert.Equal(initial.AddDays(1).UtcDateTime, user.NutritionProfile.ModifiedOnUtc),
            () => Assert.Null(user.ModifiedOnUtc),
            () => Assert.Null(user.Preferences.ModifiedOnUtc));

        using IDisposable repeatClock = DomainTime.Override(new FixedTimeProvider(initial.AddDays(2)));
        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Clear<ProfileHeightCm>()));
        Assert.Equal(initial.AddDays(1).UtcDateTime, user.NutritionProfile.ModifiedOnUtc);
    }

    [Fact]
    public void OmittedHeight_PreservesMeasurementAcrossSemanticAndCompatibilityUpdates() {
        var user = User.Create("height-preserve@example.com", "hash");
        user.UpdatePersonalInfo(height: 170);
        DateTime? audit = user.NutritionProfile.ModifiedOnUtc;

        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Unchanged<ProfileHeightCm>()));
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate());
        user.UpdatePersonalInfo(height: null);

        Assert.Multiple(
            () => Assert.Equal(170, user.HeightCm),
            () => Assert.Equal(audit, user.NutritionProfile.ModifiedOnUtc),
            () => Assert.Null(user.ModifiedOnUtc));

        user.UpdatePersonalInfoChanges(Changes(FieldChanges.Set(ProfileHeightCm.Create(181.2))));
        Assert.Equal(181.2, user.HeightCm);
    }

    private static UserPersonalInfoChanges Changes(FieldChange<ProfileHeightCm> height) =>
        new(Username: null, FirstName: null, LastName: null, FieldChanges.Unchanged<ProfileBirthDate>(), Gender: null, WeightKg: null, height);

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
