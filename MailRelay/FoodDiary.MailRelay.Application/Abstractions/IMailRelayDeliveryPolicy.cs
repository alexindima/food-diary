namespace FoodDiary.MailRelay.Application.Abstractions;

public interface IMailRelayDeliveryPolicy {
    Result CanEnqueue(RelayEmailMessageRequest request);
}
