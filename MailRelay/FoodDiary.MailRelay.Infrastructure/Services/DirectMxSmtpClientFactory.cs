namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class DirectMxSmtpClientFactory : IDirectMxSmtpClientFactory {
    public IDirectMxSmtpClient Create() => new DirectMxSmtpClientAdapter();
}
