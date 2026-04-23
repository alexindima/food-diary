using FoodDiary.MailRelay.Options;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayDkimOptionsTests {
    [Fact]
    public void HasValidConfiguration_WhenDisabled_ReturnsTrue() {
        var options = new MailRelayDkimOptions {
            Enabled = false
        };

        Assert.True(MailRelayDkimOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenEnabledWithoutSelector_ReturnsFalse() {
        var options = new MailRelayDkimOptions {
            Enabled = true,
            Domain = "mail.example.com",
            PrivateKeyPem = "pem"
        };

        Assert.False(MailRelayDkimOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenEnabledWithBothKeySources_ReturnsFalse() {
        var options = new MailRelayDkimOptions {
            Enabled = true,
            Domain = "mail.example.com",
            Selector = "fd1",
            PrivateKeyPem = "pem",
            PrivateKeyPath = "/secrets/dkim.pem"
        };

        Assert.False(MailRelayDkimOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenEnabledWithInlinePem_ReturnsTrue() {
        var options = new MailRelayDkimOptions {
            Enabled = true,
            Domain = "mail.example.com",
            Selector = "fd1",
            PrivateKeyPem = "pem"
        };

        Assert.True(MailRelayDkimOptions.HasValidConfiguration(options));
    }
}
