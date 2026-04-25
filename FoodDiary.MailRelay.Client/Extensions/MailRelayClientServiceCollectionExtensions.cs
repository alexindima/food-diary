using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Client.Extensions;

public static class MailRelayClientServiceCollectionExtensions {
    public static IServiceCollection AddMailRelayClient(
        this IServiceCollection services,
        Action<MailRelayClientOptions> configureOptions) {
        services.AddOptions<MailRelayClientOptions>()
            .Configure(configureOptions)
            .Validate(MailRelayClientOptions.HasValidBaseUrl,
                "Mail relay client base URL must be an absolute URL.")
            .Validate(static options => options.Timeout > TimeSpan.Zero,
                "Mail relay client timeout must be greater than zero.")
            .ValidateOnStart();

        services.AddHttpClient<IMailRelayClient, MailRelayClient>((sp, client) => {
            var options = sp.GetRequiredService<IOptions<MailRelayClientOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl)) {
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            }

            client.Timeout = options.Timeout;
        });

        return services;
    }
}
