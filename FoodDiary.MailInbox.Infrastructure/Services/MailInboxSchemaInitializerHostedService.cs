using FoodDiary.MailInbox.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxSchemaInitializerHostedService(
    IMailInboxSchemaInitializer schemaInitializer,
    ILogger<MailInboxSchemaInitializerHostedService> logger) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        await schemaInitializer.EnsureSchemaAsync(cancellationToken);
        logger.LogInformation("Mail inbox schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
