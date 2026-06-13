using FoodDiary.MailRelay.Application.Common.Results;
using Microsoft.Extensions.Options;
using MimeKit;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public sealed class ConfiguredMailRelayDeliveryPolicy(IOptions<MailRelayDeliveryOptions> options) : IMailRelayDeliveryPolicy {
    private readonly MailRelayDeliveryOptions _options = options.Value;

    public Result CanEnqueue(RelayEmailMessageRequest request) {
        if (!string.Equals(_options.Mode, MailRelayDeliveryOptions.DirectMxMode, StringComparison.Ordinal)) {
            return Result.Success();
        }

        int domainCount = request.To
            .Select(static recipient => MailboxAddress.Parse(recipient).Domain)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        return domainCount <= 1
            ? Result.Success()
            : Result.Failure(MailRelayErrors.DirectMxRequiresSingleRecipientDomain());
    }
}
