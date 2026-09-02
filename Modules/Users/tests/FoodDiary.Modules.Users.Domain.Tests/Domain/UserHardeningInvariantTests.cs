using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserHardeningInvariantTests {
    [Fact]
    public void User_BirthDateIsStoredAsUtcDateAndGoogleLinkingIsAtomic() {
        var user = User.Create("user@example.com", "hashed-password");
        var localBirthDate = new DateTime(1990, 5, 12, 18, 30, 0, DateTimeKind.Local);
        user.UpdatePersonalInfo(birthDate: localBirthDate);
        user.LinkGoogleIdentity("issuer", "subject");
        DateTime storedBirthDate = user.BirthDate.GetValueOrDefault();

        Assert.Throws<ArgumentException>(() => user.LinkGoogleIdentity("changed", " "));

        Assert.Multiple(
            () => Assert.True(user.BirthDate.HasValue),
            () => Assert.Equal(DateTimeKind.Utc, storedBirthDate.Kind),
            () => Assert.Equal(localBirthDate.ToUniversalTime().Date, storedBirthDate),
            () => Assert.Equal("issuer", user.GoogleIssuer),
            () => Assert.Equal("subject", user.GoogleSubject));
    }
}
