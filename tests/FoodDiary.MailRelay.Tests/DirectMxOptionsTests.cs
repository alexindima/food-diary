using FoodDiary.MailRelay.Infrastructure.Options;

namespace FoodDiary.MailRelay.Tests;

public sealed class DirectMxOptionsTests {
    [Fact]
    public void HasValidConfiguration_WhenValuesArePositive_ReturnsTrue() {
        var options = new DirectMxOptions {
            Port = 25,
            ConnectTimeoutSeconds = 20
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
}
