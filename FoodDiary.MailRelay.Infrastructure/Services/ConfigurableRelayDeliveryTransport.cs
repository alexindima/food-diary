using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class ConfigurableRelayDeliveryTransport(
    SmtpRelayDeliveryTransport smtpTransport,
    DirectMxRelayDeliveryTransport directMxTransport,
    IOptions<MailRelayDeliveryOptions> options) : IRelayDeliveryTransport {
    private readonly MailRelayDeliveryOptions _options = options.Value;

    public Task SendAsync(RelayEmailMessageRequest request, CancellationToken cancellationToken) {
        return ResolveTransport().SendAsync(request, cancellationToken);
    }

    private IRelayDeliveryTransport ResolveTransport() {
        return _options.Mode switch {
            MailRelayDeliveryOptions.SmtpSubmissionMode => smtpTransport,
            MailRelayDeliveryOptions.DirectMxMode => directMxTransport,
            _ => throw new InvalidOperationException($"Unsupported mail relay delivery mode '{_options.Mode}'.")
        };
    }
}
