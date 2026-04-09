namespace FoodDiary.Application.Authentication.Common;

public interface IEmailSender {
    Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken);
}
