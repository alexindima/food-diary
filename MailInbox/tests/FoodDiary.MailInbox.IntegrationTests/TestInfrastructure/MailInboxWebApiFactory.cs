using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.MailInbox.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
public sealed class MailInboxWebApiFactory : WebApplicationFactory<global::Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.UseEnvironment(Environments.Development);
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureAppConfiguration((_, configuration) => {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=mailbox;Username=postgres;Password=integration-test-password",
                ["MailInboxHttp:MetadataApiKey"] = "fedcba9876543210fedcba987654321a",
                ["MailInboxHttp:ContentApiKey"] = "fedcba9876543210fedcba987654321b",
                ["MailInboxHttp:StateApiKey"] = "fedcba9876543210fedcba987654321c",
                ["MailInboxSmtp:Enabled"] = "false",
            });
        });

        builder.ConfigureServices(services => {
            ServiceDescriptor[] hostedServices = [.. services
                .Where(static descriptor =>
                    descriptor.ServiceType == typeof(IHostedService) &&
                    (descriptor.ImplementationType == typeof(MailInboxRetentionHostedService) ||
                     descriptor.ImplementationType == typeof(MailInboxSmtpHostedService)))];

            foreach (ServiceDescriptor hostedService in hostedServices) {
                services.Remove(hostedService);
            }

            services.RemoveAll<IMailInboxReadinessChecker>();
            services.AddSingleton<IMailInboxReadinessChecker, ReadyMailInboxChecker>();
        });
    }

    private sealed class ReadyMailInboxChecker : IMailInboxReadinessChecker {
        public Task CheckReadyAsync(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
