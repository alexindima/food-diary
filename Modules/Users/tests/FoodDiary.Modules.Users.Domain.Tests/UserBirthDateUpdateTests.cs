using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserBirthDateUpdateTests {
    private static readonly DateTime OriginalDate = new(2000, 10, 2, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void UpdatePersonalInfo_ExplicitNullClearsDateWithoutChangingOtherFields() {
        var user = User.Create("birthdate@example.com", "hash");
        user.UpdatePersonalInfo(firstName: "QA", birthDate: OriginalDate, height: 178);

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(BirthDateSpecified: true));

        Assert.Multiple(
            () => Assert.Null(user.BirthDate),
            () => Assert.Equal("QA", user.FirstName),
            () => Assert.Equal(178, user.HeightCm));
    }

    [Fact]
    public void UpdatePersonalInfo_OmittedDatePreservesExistingValue() {
        var user = User.Create("birthdate@example.com", "hash");
        user.UpdatePersonalInfo(birthDate: OriginalDate);

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(FirstName: "Updated"));

        Assert.Multiple(
            () => Assert.Equal(OriginalDate, user.BirthDate),
            () => Assert.Equal("Updated", user.FirstName));
    }

    [Fact]
    public void UpdatePersonalInfo_SelectedDateStillUpdatesWithoutPresenceMetadata() {
        var user = User.Create("birthdate@example.com", "hash");

        user.UpdatePersonalInfo(new UserPersonalInfoUpdate(BirthDate: OriginalDate));

        Assert.Equal(OriginalDate, user.BirthDate);
    }

    [Fact]
    public void UpdatePersonalInfo_SpecifiedFutureDateDoesNotBypassValidation() {
        var user = User.Create("birthdate@example.com", "hash");
        user.UpdatePersonalInfo(birthDate: OriginalDate);

        Assert.Throws<ArgumentOutOfRangeException>(() => user.UpdatePersonalInfo(new UserPersonalInfoUpdate(
            FirstName: "Invalid", BirthDate: DateTime.UtcNow.Date.AddDays(1), BirthDateSpecified: true)));

        Assert.Multiple(
            () => Assert.Equal(OriginalDate, user.BirthDate),
            () => Assert.Null(user.FirstName));
    }
}
