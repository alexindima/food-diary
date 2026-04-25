using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Client.Extensions;

public static class MailInboxClientServiceCollectionExtensions {
    public static IServiceCollection AddMailInboxClient(
        this IServiceCollection services,
        Action<MailInboxClientOptions> configureOptions) {
        services.AddOptions<MailInboxClientOptions>()
            .Configure(configureOptions)
            .Validate(MailInboxClientOptions.HasValidBaseUrl,
                "MailInbox client base URL must be an absolute URL.")
            .Validate(static options => options.Timeout > TimeSpan.Zero,
                "MailInbox client timeout must be greater than zero.")
            .ValidateOnStart();

        services.AddHttpClient<IMailInboxClient, MailInboxClient>((sp, client) => {
            var options = sp.GetRequiredService<IOptions<MailInboxClientOptions>>().Value;
            if (!string.IsNullOrWhiteSpace(options.BaseUrl)) {
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            }

            client.Timeout = options.Timeout;
        });

        return services;
    }
}
