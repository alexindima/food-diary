using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationInputTests {
    private static readonly DateTime Now = new(2026, 8, 19, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void LinkAndUserFacingValues_RejectInvalidInput() {
        Assert.Throws<ArgumentException>(() => DietologistInvitation.Create(
            UserId.New(), "not-an-email", "hash", Now.AddDays(1), DietologistPermissions.AllEnabled));
    }
}
