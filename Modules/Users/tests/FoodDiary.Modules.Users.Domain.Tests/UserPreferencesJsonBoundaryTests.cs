using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserPreferencesJsonBoundaryTests {
    [Fact]
    public void JsonBackedValues_RejectInvalidJson() {
        var user = User.Create("json@example.com", "hash");
        Assert.Throws<ArgumentException>(() => user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: "{invalid")));
    }

    [Fact]
    public void JsonBackedValues_RejectValidJsonAboveDomainLimit() {
        string oversizedJson = $"\"{new string('x', 65536)}\"";
        var user = User.Create("json-size@example.com", "hash");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: oversizedJson)));
    }

    [Fact]
    public void JsonBackedValues_CountLeadingWhitespaceTowardDomainLimit() {
        string oversizedJson = new string(' ', 65536) + "{}";
        var user = User.Create("json-whitespace@example.com", "hash");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(DashboardLayoutJson: oversizedJson)));
    }
}
