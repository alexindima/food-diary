using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.ComponentModel;
using SmtpServer.Storage;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxSmtpHostedService(
    IOptions<MailInboxSmtpOptions> options,
    SmtpInboundMessageStore messageStore,
    MailInboxMailboxFilter mailboxFilter,
    ILogger<MailInboxSmtpHostedService> logger) : BackgroundService {
    private readonly MailInboxSmtpOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_options.Enabled) {
            logger.LogInformation("Mail inbox SMTP listener is disabled.");
            return;
        }

        var serverOptions = new SmtpServerOptionsBuilder()
            .ServerName(_options.ServerName)
            .Endpoint(endpoint => endpoint.Port(_options.Port))
            .MaxMessageSize(_options.MaxMessageSizeBytes, MaxMessageSizeHandling.Strict)
            .Build();

        var serviceProvider = new ServiceProvider();
        serviceProvider.Add(new DelegatingMessageStoreFactory(_ => messageStore));
        serviceProvider.Add(new DelegatingMailboxFilterFactory(_ => mailboxFilter));

        var server = new SmtpServer.SmtpServer(serverOptions, serviceProvider);
        logger.LogInformation(
            "Mail inbox SMTP listener starting. ServerName={ServerName}; Port={Port}; MaxMessageSizeBytes={MaxMessageSizeBytes}",
            _options.ServerName,
            _options.Port,
            _options.MaxMessageSizeBytes);

        await server.StartAsync(stoppingToken);
    }
}
