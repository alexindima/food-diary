namespace FoodDiary.MailRelay.Application.Emails.Services;

public sealed class NoOpMailRelayDeliveryPolicy : IMailRelayDeliveryPolicy {
    public Result CanEnqueue(RelayEmailMessageRequest request) => Result.Success();
}
