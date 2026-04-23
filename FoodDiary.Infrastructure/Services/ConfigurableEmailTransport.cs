using System.Net.Mail;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

internal sealed class ConfigurableEmailTransport(
    SmtpClientEmailTransport smtpTransport,
    RelayEmailTransport relayTransport,
    IOptions<EmailDeliveryOptions> deliveryOptions) : IEmailTransport {
    private readonly EmailDeliveryOptions _deliveryOptions = deliveryOptions.Value;

    public Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
        return ResolveTransport().SendAsync(message, cancellationToken);
    }

    private IEmailTransport ResolveTransport() {
        return _deliveryOptions.Mode switch {
            EmailDeliveryOptions.SmtpMode => smtpTransport,
            EmailDeliveryOptions.RelayMode => relayTransport,
            _ => throw new InvalidOperationException($"Unsupported email delivery mode '{_deliveryOptions.Mode}'.")
        };
    }
}
