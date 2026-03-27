using FoodDiary.JobManager.Services;

namespace FoodDiary.JobManager.Tests;

public sealed class OptionsValidationTests {
    [Fact]
    public void ImageCleanupOptions_WithInvalidValues_FailsValidation() {
        var options = new ImageCleanupOptions {
            OlderThanHours = 0,
            BatchSize = 0,
            Cron = ""
        };

        Assert.False(ImageCleanupOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void UserCleanupOptions_WithInvalidReassignUserId_FailsValidation() {
        var options = new UserCleanupOptions {
            RetentionDays = 30,
            BatchSize = 25,
            Cron = "0 3 * * *",
            ReassignUserId = "not-a-guid"
        };

        Assert.False(UserCleanupOptions.HasValidConfiguration(options));
    }

    [Fact]
    public void UserCleanupOptions_WithValidValues_PassesValidation() {
        var options = new UserCleanupOptions {
            RetentionDays = 30,
            BatchSize = 25,
            Cron = "0 3 * * *",
            ReassignUserId = Guid.NewGuid().ToString()
        };

        Assert.True(UserCleanupOptions.HasValidConfiguration(options));
    }
}
