using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Infrastructure.Integrations.MailInbox;
using FoodDiary.MailInbox.Client.Extensions;
using FoodDiary.MailInbox.Client.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Integrations;

public static class AdminMailInboxIntegration {
    private static readonly string[] ConfigurationKeys = [
        "BaseUrl",
        "MetadataApiKey",
        "ContentApiKey",
        "StateApiKey",
        "AllowInsecureLoopback",
    ];

    public static IServiceCollection AddAdminMailInboxIntegration(
        this IServiceCollection services,
        IConfiguration configuration) {
        IConfigurationSection section = configuration.GetSection(MailInboxClientOptions.SectionName);
        if (ConfigurationKeys.Any(key => section[key] is not null)) {
            services.AddMailInboxClient(options => {
                options.BaseUrl = section["BaseUrl"] ?? string.Empty;
                options.MetadataApiKey = section["MetadataApiKey"] ?? string.Empty;
                options.ContentApiKey = section["ContentApiKey"] ?? string.Empty;
                options.StateApiKey = section["StateApiKey"] ?? string.Empty;
                options.Timeout = TimeSpan.FromSeconds(15);
                options.AllowInsecureLoopback = section.GetValue<bool>("AllowInsecureLoopback");
            });
            services.AddScoped<IAdminMailInboxReader, MailInboxClientAdminMailInboxReader>();
        }

        return services;
    }
}
