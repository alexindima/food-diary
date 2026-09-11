using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Domain.Tests.ValueObjects;

[ExcludeFromCodeCoverage]
public sealed class SurfaceStyleCodeTests {
    [Theory]
    [InlineData("normal", "normal")]
    [InlineData(" MATTE ", "matte")]
    [InlineData("Glass", "glass")]
    public void TryParse_NormalizesSupportedValues(string input, string expected) {
        Assert.True(SurfaceStyleCode.TryParse(input, out SurfaceStyleCode value));
        Assert.Equal(expected, value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("transparent")]
    public void TryParse_RejectsUnsupportedValues(string? input) {
        Assert.False(SurfaceStyleCode.TryParse(input, out _));
    }

    [Fact]
    public void InvalidMaterial_DoesNotPartiallyApplyPreferences() {
        var user = User.Create("surface@example.com", "hash");
        string? originalTheme = user.Theme;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.UpdatePreferences(new UserPreferenceUpdate(Theme: "dark", SurfaceStyle: "invalid")));
        Assert.Equal(originalTheme, user.Theme);
        Assert.Equal("normal", user.SurfaceStyle);
    }
}
