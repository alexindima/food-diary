using FoodDiary.Application.Common.Models;

namespace FoodDiary.Application.Common.Interfaces.Services;

public interface IEmailSender {
    Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken);
    Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken);
}
