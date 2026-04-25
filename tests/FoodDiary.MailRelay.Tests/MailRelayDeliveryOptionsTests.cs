using FoodDiary.MailRelay.Infrastructure.Options;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayDeliveryOptionsTests {
    [Theory]
    [InlineData(MailRelayDeliveryOptions.SmtpSubmissionMode)]
    [InlineData(MailRelayDeliveryOptions.DirectMxMode)]
    public void HasSupportedMode_WhenModeIsSupported_ReturnsTrue(string mode) {
        var options = new MailRelayDeliveryOptions {
            Mode = mode
        };

        Assert.True(MailRelayDeliveryOptions.HasSupportedMode(options));
    }

    [Fact]
    public void HasSupportedMode_WhenModeIsUnknown_ReturnsFalse() {
        var options = new MailRelayDeliveryOptions {
            Mode = "Unknown"
        };

        Assert.False(MailRelayDeliveryOptions.HasSupportedMode(options));
    }
}
