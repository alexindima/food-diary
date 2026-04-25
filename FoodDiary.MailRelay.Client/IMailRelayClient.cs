using FoodDiary.MailRelay.Client.Models;

namespace FoodDiary.MailRelay.Client;

public interface IMailRelayClient {
    Task<EnqueueMailRelayEmailResponse> EnqueueAsync(
        EnqueueMailRelayEmailRequest request,
        CancellationToken cancellationToken);
}
