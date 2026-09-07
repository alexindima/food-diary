namespace FoodDiary.MailRelay.Infrastructure.Services;

public interface IDirectMxSmtpClientFactory {
    IDirectMxSmtpClient Create();
}
