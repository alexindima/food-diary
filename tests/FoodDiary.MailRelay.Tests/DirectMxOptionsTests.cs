using FoodDiary.MailRelay.Infrastructure.Options;

namespace FoodDiary.MailRelay.Tests;

public sealed class DirectMxOptionsTests {
    [Fact]
    public void HasValidConfiguration_WhenValuesArePositive_ReturnsTrue() {
        var options = new DirectMxOptions {
            Port = 25,
            ConnectTimeoutSeconds = 20,
            LocalDomain = "mail.fooddiary.club"
        };

        Assert.True(DirectMxOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenLocalDomainIsEmpty_ReturnsTrue() {
        var options = new DirectMxOptions {
            Port = 25,
            ConnectTimeoutSeconds = 20,
            LocalDomain = ""
        };

        Assert.True(DirectMxOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(25, 0)]
    public void HasValidConfiguration_WhenRequiredValueIsInvalid_ReturnsFalse(int port, int connectTimeoutSeconds) {
        var options = new DirectMxOptions {
            Port = port,
            ConnectTimeoutSeconds = connectTimeoutSeconds
        };

        Assert.False(DirectMxOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("http://mail.fooddiary.club")]
    [InlineData("mail_fooddiary_club")]
    public void HasValidConfiguration_WhenLocalDomainIsInvalid_ReturnsFalse(string localDomain) {
        var options = new DirectMxOptions {
            Port = 25,
            ConnectTimeoutSeconds = 20,
            LocalDomain = localDomain
        };

        Assert.False(DirectMxOptions.HasValidConfiguration(options));
    }
}
