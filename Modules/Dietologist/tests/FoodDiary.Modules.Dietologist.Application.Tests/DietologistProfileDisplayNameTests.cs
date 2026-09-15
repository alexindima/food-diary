using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Dietologist.Application.Common;

namespace FoodDiary.Modules.Dietologist.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class DietologistProfileDisplayNameTests {
    [Theory]
    [InlineData("Alex", "Doe", null, "en", "Alex Doe")]
    [InlineData(null, null, "owner@example.com", "en", "owner@example.com")]
    [InlineData(null, null, null, "ru", "Пользователь")]
    [InlineData(null, null, null, "en", "User")]
    public void Resolve_ProvidesDisplayNameForAccountsWithoutEmail(string? firstName, string? lastName, string? email, string language, string expected) {
        var profile = new UserDietologistProfileModel(Guid.NewGuid(), email, firstName, lastName, language, IsDietologist: false);
        Assert.Equal(expected, DietologistProfileDisplayName.Resolve(profile));
    }
}
