namespace FoodDiary.MailRelay.Services;

public interface IRelayDeliveryTransport {
    Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken);
}
