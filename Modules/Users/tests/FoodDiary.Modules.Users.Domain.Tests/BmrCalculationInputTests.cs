using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;
using FoodDiary.Domain.Primitives;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class BmrCalculationInputTests {
    [Theory]
    [InlineData(80, 180, 1800d)]
    [InlineData(700, 350, 9062d)]
    [InlineData(-1, 0, null)]
    [InlineData(double.NaN, 180, null)]
    public void StoredAdapter_PreservesExistingCalculationPolicy(double weight, double height, double? expected) {
        using IDisposable scope = DomainTime.Override(new CalculationTimeProvider());
        var birthDate = new DateTime(2000, 6, 1);
        Assert.Equal(expected, User.CalculateBmrFromProfile(BmrCalculationInput.FromStoredProfile(weight, height, birthDate, "M")));
    }

    [Fact]
    public void TypedMeasurements_KeepKilogramsAndCentimetersInTheirSlots() {
        using IDisposable scope = DomainTime.Override(new CalculationTimeProvider());
        var birthDate = new DateTime(2000, 6, 1);
        var input = BmrCalculationInput.FromMeasurements(ProfileWeightKg.Create(80), ProfileHeightCm.Create(180), ProfileBirthDate.FromEncodedDateTime(birthDate), "F");
        Assert.Equal(1634, User.CalculateBmrFromProfile(input));
        Assert.Null(User.CalculateBmrFromProfile(BmrCalculationInput.FromMeasurements(weight: null, height: null, ProfileBirthDate.FromEncodedDateTime(birthDate), "F")));
    }

    [ExcludeFromCodeCoverage]
    private sealed class CalculationTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);
    }
}
