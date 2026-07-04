using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Integrations.Services;
using FoodDiary.Integrations.Services.MailInbox;
using FoodDiary.MailInbox.Client.Extensions;
using FoodDiary.MailInbox.Client.Options;
using FoodDiary.MailRelay.Client.Extensions;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Integrations;

public static partial class DependencyInjection {
    private static IServiceCollection AddMailIntegrations(this IServiceCollection services, IConfiguration configuration) {
        services.AddMailRelayClient(options => {
            IConfigurationSection section = configuration.GetSection(MailRelayClientOptions.SectionName);
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddMailInboxClient(options => {
            IConfigurationSection section = configuration.GetSection(MailInboxClientOptions.SectionName);
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddScoped<IAdminMailInboxReader, MailInboxClientAdminMailInboxReader>();
        services.AddSingleton<RelayEmailTransport>();
        services.AddSingleton<IEmailTransport>(static sp => sp.GetRequiredService<RelayEmailTransport>());

        return services;
    }
}
