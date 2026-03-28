using System.Collections.Concurrent;
using FoodDiary.Application.Authentication.Common;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

public sealed class TestEmailSender : IEmailSender {
    private readonly ConcurrentDictionary<string, PasswordResetMessage> _passwordResetMessages = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, EmailVerificationMessage> _emailVerificationMessages = new(StringComparer.OrdinalIgnoreCase);

    public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        _emailVerificationMessages[message.ToEmail] = message;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        _passwordResetMessages[message.ToEmail] = message;
        return Task.CompletedTask;
    }

    public PasswordResetMessage GetRequiredPasswordResetMessage(string email) {
        return _passwordResetMessages.TryGetValue(email, out var message)
            ? message
            : throw new InvalidOperationException($"Password reset message for '{email}' was not captured.");
    }

    public void Clear() {
        _passwordResetMessages.Clear();
        _emailVerificationMessages.Clear();
    }
}
