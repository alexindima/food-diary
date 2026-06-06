using FoodDiary.Web.Api.Options;

namespace FoodDiary.Web.Api.IntegrationTests.Options;

[ExcludeFromCodeCoverage]
public sealed class InitialAdminOptionsTests {
    [Fact]
    public void HasValidConfiguration_WhenPasswordIsBlank_ReturnsTrue() {
        var options = new InitialAdminOptions {
            Email = string.Empty,
            Password = " ",
        };

        bool result = InitialAdminOptions.HasValidConfiguration(options);

        Assert.True(result);
    }

    [Fact]
    public void HasValidConfiguration_WhenPasswordAndEmailAreValid_ReturnsTrue() {
        var options = new InitialAdminOptions {
            Email = " admin@fooddiary.club ",
            Password = "StrongPassword123",
        };

        bool result = InitialAdminOptions.HasValidConfiguration(options);

        Assert.True(result);
    }

    [Theory]
    [InlineData("admin@fooddiary.club", "short")]
    [InlineData("admin@fooddiary.club", "123456")]
    [InlineData("invalid-email", "StrongPassword123")]
    [InlineData("bad\naddress", "StrongPassword123")]
    public void HasValidConfiguration_WhenPasswordOrEmailIsInvalid_ReturnsFalse(string email, string password) {
        var options = new InitialAdminOptions {
            Email = email,
            Password = password,
        };

        bool result = InitialAdminOptions.HasValidConfiguration(options);

        Assert.False(result);
    }
}
