using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class PremiumAccessEndTests {
    [Fact]
    public void SetPremiumAccessEnd_StoresAndClearsExpirationWithoutChangingRoles() {
        var user = User.Create("premium-end@example.com", "hash");
        DateTime end = new(2026, 12, 6, 10, 0, 0, DateTimeKind.Utc);

        user.SetPremiumAccessEnd(end);

        Assert.Multiple(() => {
            Assert.Equal(end, user.PremiumAccessEndsAtUtc);
            Assert.NotNull(user.ModifiedOnUtc);
            Assert.Empty(user.UserRoles);
        });
        user.SetPremiumAccessEnd(endsAtUtc: null);
        Assert.Null(user.PremiumAccessEndsAtUtc);
    }

    [Fact]
    public void SetPremiumAccessEnd_RejectsUnspecifiedTimeWithoutChangingExpiration() {
        var user = User.Create("premium-invalid@example.com", "hash");
        DateTime end = new(2026, 12, 6, 10, 0, 0, DateTimeKind.Utc);
        user.SetPremiumAccessEnd(end);

        Assert.Throws<ArgumentOutOfRangeException>(() => user.SetPremiumAccessEnd(DateTime.SpecifyKind(end, DateTimeKind.Unspecified)));
        Assert.Equal(end, user.PremiumAccessEndsAtUtc);
    }
}
