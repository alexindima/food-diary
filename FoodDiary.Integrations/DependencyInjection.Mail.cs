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
    private static void AddMailIntegrations(this IServiceCollection services, IConfiguration configuration) {
        services.AddMailRelayClient(options => {
            IConfigurationSection section = configuration.GetSection(MailRelayClientOptions.SectionName);
            options.BaseUrl = section["BaseUrl"] ?? string.Empty;
            options.ApiKey = section["ApiKey"] ?? string.Empty;
            options.Timeout = TimeSpan.FromSeconds(15);
        });

        IConfigurationSection mailInboxSection = configuration.GetSection(MailInboxClientOptions.SectionName);
        if (mailInboxSection.Exists()) {
            services.AddMailInboxClient(options => {
                options.BaseUrl = mailInboxSection["BaseUrl"] ?? string.Empty;
                options.MetadataApiKey = mailInboxSection["MetadataApiKey"] ?? string.Empty;
                options.ContentApiKey = mailInboxSection["ContentApiKey"] ?? string.Empty;
                options.StateApiKey = mailInboxSection["StateApiKey"] ?? string.Empty;
                options.Timeout = TimeSpan.FromSeconds(15);
                options.AllowInsecureLoopback = mailInboxSection.GetValue<bool>("AllowInsecureLoopback");
            });
            services.AddScoped<IAdminMailInboxReader, MailInboxClientAdminMailInboxReader>();
        }

        services.AddScoped<RelayEmailTransport>();
        services.AddScoped<IEmailTransport>(static sp => sp.GetRequiredService<RelayEmailTransport>());
    }
}
