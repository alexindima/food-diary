using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Modules.Dietologist.Domain.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Dietologist.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistInvitationStorageTests {
    [Fact]
    public void StorageBoundValues_AreValidatedAndUtcNormalized() {
        Assert.Throws<ArgumentOutOfRangeException>(() => DietologistInvitation.Create(
            UserId.New(), new string('x', 257), "hash", DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled));
        Assert.Throws<ArgumentOutOfRangeException>(() => DietologistInvitation.Create(
            UserId.New(), "diet@example.com", new string('x', 257), DateTime.UtcNow.AddDays(1), DietologistPermissions.AllEnabled));
        Assert.Throws<ArgumentOutOfRangeException>(() => DietologistInvitation.Create(
            UserId.New(), "diet@example.com", "hash", new DateTime(2026, 1, 1), DietologistPermissions.AllEnabled));
    }
}
