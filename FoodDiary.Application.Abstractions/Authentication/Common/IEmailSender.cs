namespace FoodDiary.Application.Abstractions.Authentication.Common;

public interface IEmailSender {
    Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken);
    Task SendTestEmailAsync(TestEmailMessage message, CancellationToken cancellationToken);
}
