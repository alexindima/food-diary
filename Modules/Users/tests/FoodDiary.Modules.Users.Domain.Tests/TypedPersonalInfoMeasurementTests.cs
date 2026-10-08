using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class TypedPersonalInfoMeasurementTests {
    [Fact]
    public void TypedMeasurements_TouchOnlyTheNutritionProfileAndPreserveOmittedValues() {
        var user = User.Create("typed-profile@example.com", "hash");
        long securityVersion = user.SecurityVersion;
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            WeightKg: ProfileWeightKg.Create(72.125), HeightCm: ProfileHeightCm.Create(180.125)));
        DateTime? profileStamp = user.NutritionProfile.ModifiedOnUtc;
        Assert.NotNull(profileStamp);

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            WeightKg: ProfileWeightKg.Create(72.125), HeightCm: ProfileHeightCm.Create(180.125)));
        user.UpdatePersonalInfo(default(UserPersonalInfoUpdate));

        Assert.Multiple(() => {
            Assert.Equal(72.125, user.WeightKg);
            Assert.Equal(180.125, user.HeightCm);
            Assert.Equal(profileStamp, user.NutritionProfile.ModifiedOnUtc);
            Assert.Null(user.ModifiedOnUtc);
            Assert.Null(user.Preferences.ModifiedOnUtc);
            Assert.Equal(securityVersion, user.SecurityVersion);
        });
    }

    [Fact]
    public void TypedMeasurements_KeepExplicitBirthDateClearingIndependent() {
        var user = User.Create("typed-birth-date@example.com", "hash");
        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            BirthDate: new DateTime(1990, 2, 28, 0, 0, 0, DateTimeKind.Utc),
            WeightKg: ProfileWeightKg.Create(72), HeightCm: ProfileHeightCm.Create(180)));

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(BirthDate: null, BirthDateSpecified: true));

        Assert.Multiple(() => {
            Assert.Null(user.BirthDate);
            Assert.Equal(72, user.WeightKg);
            Assert.Equal(180, user.HeightCm);
            Assert.Null(user.ModifiedOnUtc);
        });
    }

    [Fact]
    public void ScalarCompatibility_PreservesValidationOrderAndParameterNames() {
        var user = User.Create("scalar-profile@example.com", "hash");
        ArgumentOutOfRangeException birthError = Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(birthDate: DateTime.UtcNow.AddDays(2), weight: double.NaN, height: double.NaN));
        ArgumentOutOfRangeException weightError = Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(weight: double.NaN, height: double.NaN));
        ArgumentOutOfRangeException heightError = Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePersonalInfo(weight: 72, height: double.NaN));

        Assert.Multiple(() => {
            Assert.Equal("birthDate", birthError.ParamName);
            Assert.Equal("weight", weightError.ParamName);
            Assert.Equal("height", heightError.ParamName);
            Assert.Null(user.WeightKg);
            Assert.Null(user.HeightCm);
            Assert.Null(user.NutritionProfile.ModifiedOnUtc);
            Assert.Null(user.ModifiedOnUtc);
        });
    }

    [Fact]
    public void ProfileMeasurements_CannotBeCreatedWithAnInvalidDefaultZero() {
        Assert.Multiple(() => {
            Assert.False(typeof(ProfileWeightKg).IsValueType);
            Assert.False(typeof(ProfileHeightCm).IsValueType);
            Assert.Empty(typeof(ProfileWeightKg).GetConstructors());
            Assert.Empty(typeof(ProfileHeightCm).GetConstructors());
            Assert.Equal(500, ProfileWeightKg.Create(500).Value);
            Assert.Equal(300, ProfileHeightCm.Create(300).Value);
        });
    }
}
