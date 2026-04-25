using FoodDiary.MailInbox.Infrastructure.Options;

namespace FoodDiary.MailInbox.Tests;

public sealed class MailInboxSmtpOptionsTests {
    [Fact]
    public void HasValidConfiguration_WhenValuesAreValid_ReturnsTrue() {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = 2525,
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = ["admin@fooddiary.club"]
        };

        Assert.True(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(0, 1024)]
    [InlineData(2525, 0)]
    public void HasValidConfiguration_WhenRequiredNumericValueIsInvalid_ReturnsFalse(
        int port,
        int maxMessageSizeBytes) {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = port,
            MaxMessageSizeBytes = maxMessageSizeBytes,
            AllowedRecipients = ["admin@fooddiary.club"]
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void HasValidConfiguration_WhenRecipientsAreEmpty_ReturnsFalse() {
        var options = new MailInboxSmtpOptions {
            ServerName = "mail.fooddiary.club",
            Port = 2525,
            MaxMessageSizeBytes = 1024,
            AllowedRecipients = []
        };

        Assert.False(MailInboxSmtpOptions.HasValidConfiguration(options));
    }
}
