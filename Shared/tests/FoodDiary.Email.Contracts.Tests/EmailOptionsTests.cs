using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Email.Contracts.Tests;

[ExcludeFromCodeCoverage]
public sealed class EmailOptionsTests {
    [Theory]
    [InlineData("/verify-email", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void EmailOptions_HasVerificationPath_ReturnsWhetherPathIsConfigured(string verificationPath, bool expected) {
        var options = new EmailOptions {
            VerificationPath = verificationPath,
        };

        Assert.Equal(expected, EmailOptions.HasVerificationPath(options));
    }

    [Theory]
    [InlineData("/reset-password", true)]
    [InlineData("", false)]
    [InlineData(" ", false)]
    public void EmailOptions_HasPasswordResetPath_ReturnsWhetherPathIsConfigured(string passwordResetPath, bool expected) {
        var options = new EmailOptions {
            PasswordResetPath = passwordResetPath,
        };

        Assert.Equal(expected, EmailOptions.HasPasswordResetPath(options));
    }
}
