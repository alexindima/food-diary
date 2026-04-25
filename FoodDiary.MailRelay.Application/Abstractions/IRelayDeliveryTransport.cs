namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IRelayDeliveryTransport {
    Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken);
}
