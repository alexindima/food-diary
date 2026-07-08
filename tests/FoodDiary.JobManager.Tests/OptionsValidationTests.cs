using FoodDiary.JobManager.Services;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class OptionsValidationTests {
    [Fact]
    public void ImageCleanupOptions_WithInvalidValues_FailsValidation() {
        var options = new ImageCleanupOptions {
            OlderThanHours = 0,
            BatchSize = 0,
            Cron = "",
        };

        Assert.False(ImageCleanupOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void UserCleanupOptions_WithInvalidReassignUserId_FailsValidation() {
        var options = new UserCleanupOptions {
            RetentionDays = 30,
            BatchSize = 25,
            Cron = "0 3 * * *",
            ReassignUserId = "not-a-guid",
        };

        Assert.False(UserCleanupOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void UserCleanupOptions_WithValidValues_PassesValidation() {
        var options = new UserCleanupOptions {
            RetentionDays = 30,
            BatchSize = 25,
            Cron = "0 3 * * *",
            ReassignUserId = Guid.NewGuid().ToString(),
        };

        Assert.True(UserCleanupOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(true, 180, 500, "0 3 * * *", true)]
    [InlineData(false, 0, 0, "", true)]
    [InlineData(true, 0, 500, "0 3 * * *", false)]
    [InlineData(true, 180, 0, "0 3 * * *", false)]
    [InlineData(true, 180, 500, "", false)]
    [InlineData(true, 180, 500, " ", false)]
    public void UserLoginEventCleanupOptions_ReturnExpectedValidationResult(
        bool enabled,
        int retentionDays,
        int batchSize,
        string cron,
        bool expected) {
        var options = new UserLoginEventCleanupOptions {
            Enabled = enabled,
            RetentionDays = retentionDays,
            BatchSize = batchSize,
            Cron = cron,
        };

        Assert.Equal(expected, UserLoginEventCleanupOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void BillingRenewalOptions_WhenDisabled_PassesValidation() {
        var options = new BillingRenewalOptions {
            Enabled = false,
            Provider = string.Empty,
            BatchSize = 0,
            Cron = string.Empty,
        };

        Assert.True(BillingRenewalOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void BillingRenewalOptions_WithValidEnabledValues_PassesValidation() {
        var options = new BillingRenewalOptions {
            Enabled = true,
            Provider = "YooKassa",
            BatchSize = 50,
            Cron = "15 * * * *",
        };

        Assert.True(BillingRenewalOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData("", 50, "15 * * * *")]
    [InlineData(" ", 50, "15 * * * *")]
    [InlineData("YooKassa", 0, "15 * * * *")]
    [InlineData("YooKassa", 50, "")]
    [InlineData("YooKassa", 50, " ")]
    public void BillingRenewalOptions_WithInvalidEnabledValues_FailsValidation(
        string provider,
        int batchSize,
        string cron) {
        var options = new BillingRenewalOptions {
            Enabled = true,
            Provider = provider,
            BatchSize = batchSize,
            Cron = cron,
        };

        Assert.False(BillingRenewalOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(true, "* * * * *", true)]
    [InlineData(false, "", true)]
    [InlineData(true, "", false)]
    [InlineData(true, " ", false)]
    public void FastingNotificationOptions_ReturnExpectedValidationResult(
        bool enabled,
        string cron,
        bool expected) {
        var options = new FastingNotificationOptions {
            Enabled = enabled,
            Cron = cron,
        };

        Assert.Equal(expected, FastingNotificationOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(true, 25, "* * * * *", true)]
    [InlineData(false, 0, "", true)]
    [InlineData(true, 0, "* * * * *", false)]
    [InlineData(true, 25, "", false)]
    [InlineData(true, 25, " ", false)]
    public void ImageObjectDeletionOutboxOptions_ReturnExpectedValidationResult(
        bool enabled,
        int batchSize,
        string cron,
        bool expected) {
        var options = new ImageObjectDeletionOutboxOptions {
            Enabled = enabled,
            BatchSize = batchSize,
            Cron = cron,
        };

        Assert.Equal(expected, ImageObjectDeletionOutboxOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(true, 50, "* * * * *", true)]
    [InlineData(false, 0, "", true)]
    [InlineData(true, 0, "* * * * *", false)]
    [InlineData(true, 50, "", false)]
    [InlineData(true, 50, " ", false)]
    public void EmailOutboxOptions_ReturnExpectedValidationResult(
        bool enabled,
        int batchSize,
        string cron,
        bool expected) {
        var options = new EmailOutboxOptions {
            Enabled = enabled,
            BatchSize = batchSize,
            Cron = cron,
        };

        Assert.Equal(expected, EmailOutboxOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(true, 50, "* * * * *", true)]
    [InlineData(false, 0, "", true)]
    [InlineData(true, 0, "* * * * *", false)]
    [InlineData(true, 50, "", false)]
    [InlineData(true, 50, " ", false)]
    public void NotificationWebPushOutboxOptions_ReturnExpectedValidationResult(
        bool enabled,
        int batchSize,
        string cron,
        bool expected) {
        var options = new NotificationWebPushOutboxOptions {
            Enabled = enabled,
            BatchSize = batchSize,
            Cron = cron,
        };

        Assert.Equal(expected, NotificationWebPushOutboxOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void NotificationCleanupOptions_WithValidValues_PassesValidation() {
        var options = new NotificationCleanupOptions {
            TransientTypes = ["FastingCheckInReminder"],
            TransientReadRetentionDays = 14,
            TransientUnreadRetentionDays = 30,
            StandardReadRetentionDays = 60,
            StandardUnreadRetentionDays = 90,
            BatchSize = 100,
            Cron = "0 4 * * *",
        };

        Assert.True(NotificationCleanupOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(0, 30, 60, 90, 100, "0 4 * * *")]
    [InlineData(14, 0, 60, 90, 100, "0 4 * * *")]
    [InlineData(14, 30, 0, 90, 100, "0 4 * * *")]
    [InlineData(14, 30, 60, 0, 100, "0 4 * * *")]
    [InlineData(14, 30, 60, 90, 0, "0 4 * * *")]
    [InlineData(14, 30, 60, 90, 100, "")]
    [InlineData(14, 30, 60, 90, 100, " ")]
    public void NotificationCleanupOptions_WithInvalidValues_FailsValidation(
        int transientReadRetentionDays,
        int transientUnreadRetentionDays,
        int standardReadRetentionDays,
        int standardUnreadRetentionDays,
        int batchSize,
        string cron) {
        var options = new NotificationCleanupOptions {
            TransientTypes = ["FastingCheckInReminder"],
            TransientReadRetentionDays = transientReadRetentionDays,
            TransientUnreadRetentionDays = transientUnreadRetentionDays,
            StandardReadRetentionDays = standardReadRetentionDays,
            StandardUnreadRetentionDays = standardUnreadRetentionDays,
            BatchSize = batchSize,
            Cron = cron,
        };

        Assert.False(NotificationCleanupOptions.HasValidConfiguration(options));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void NotificationCleanupOptions_WithInvalidTransientType_FailsValidation(string? transientType) {
        var options = new NotificationCleanupOptions {
            TransientTypes = [transientType!],
            TransientReadRetentionDays = 14,
            TransientUnreadRetentionDays = 30,
            StandardReadRetentionDays = 60,
            StandardUnreadRetentionDays = 90,
            BatchSize = 100,
            Cron = "0 4 * * *",
        };

        Assert.False(NotificationCleanupOptions.HasValidConfiguration(options));
    }
}
