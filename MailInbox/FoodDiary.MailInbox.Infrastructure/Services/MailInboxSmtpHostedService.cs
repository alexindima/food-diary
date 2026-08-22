using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.ComponentModel;
using SmtpServer.Storage;

namespace FoodDiary.MailInbox.Infrastructure.Services;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public sealed class MailInboxSmtpHostedService(
    IOptions<MailInboxSmtpOptions> options,
    SmtpInboundMessageStore messageStore,
    MailInboxMailboxFilter mailboxFilter,
    MailInboxEndpointListenerFactory endpointListenerFactory,
    ILogger<MailInboxSmtpHostedService> logger,
    TimeProvider? timeProvider = null) : BackgroundService {
    private readonly MailInboxSmtpOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        if (!_options.Enabled) {
            logger.LogInformation("Mail inbox SMTP listener is disabled.");
            return;
        }

        using X509Certificate2 certificate = LoadServerCertificate();
        ISmtpServerOptions serverOptions = new SmtpServerOptionsBuilder()
            .ServerName(_options.ServerName)
            .Endpoint(endpoint => endpoint
                .Endpoint(new IPEndPoint(IPAddress.Parse(_options.ListenAddress), _options.Port))
                .Certificate(certificate)
                .SupportedSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13)
                .SessionTimeout(_options.SessionTimeout))
            .MaxMessageSize(_options.MaxMessageSizeBytes, MaxMessageSizeHandling.Strict)
            .Build();

        var serviceProvider = new ServiceProvider();
        serviceProvider.Add(new DelegatingMessageStoreFactory(_ => messageStore));
        serviceProvider.Add(new DelegatingMailboxFilterFactory(_ => mailboxFilter));
        serviceProvider.Add(endpointListenerFactory);

        var server = new SmtpServer.SmtpServer(serverOptions, serviceProvider);
        logger.LogInformation(
            "Mail inbox SMTP listener starting with STARTTLS. ServerName={ServerName}; ListenAddress={ListenAddress}; Port={Port}; CertificateExpiresUtc={CertificateExpiresUtc}; MaxMessageSizeBytes={MaxMessageSizeBytes}",
            _options.ServerName,
            _options.ListenAddress,
            _options.Port,
            certificate.NotAfter.ToUniversalTime(),
            _options.MaxMessageSizeBytes);

        await server.StartAsync(stoppingToken).ConfigureAwait(false);
    }

    private X509Certificate2 LoadServerCertificate() {
        X509Certificate2 certificate;
        try {
            using var pemCertificate = X509Certificate2.CreateFromPemFile(
                _options.CertificatePath,
                _options.PrivateKeyPath);
            byte[] pkcs12 = pemCertificate.Export(X509ContentType.Pkcs12);
            try {
                certificate = X509CertificateLoader.LoadPkcs12(
                    pkcs12,
                    string.Empty,
                    OperatingSystem.IsWindows()
                        ? X509KeyStorageFlags.UserKeySet
                        : X509KeyStorageFlags.EphemeralKeySet,
                    Pkcs12LoaderLimits.Defaults);
            } finally {
                CryptographicOperations.ZeroMemory(pkcs12);
            }
        } catch (Exception exception) when (exception is IOException or CryptographicException or ArgumentException) {
            throw new InvalidOperationException(
                "Mail inbox SMTP TLS certificate could not be loaded.",
                exception);
        }

        try {
            if (!certificate.HasPrivateKey) {
                throw new InvalidOperationException("Mail inbox SMTP TLS certificate has no private key.");
            }

            DateTime utcNow = _timeProvider.GetUtcNow().UtcDateTime;
            if (utcNow < certificate.NotBefore.ToUniversalTime() ||
                utcNow >= certificate.NotAfter.ToUniversalTime()) {
                throw new InvalidOperationException("Mail inbox SMTP TLS certificate is not currently valid.");
            }

            if (!certificate.MatchesHostname(
                    _options.ServerName,
                    allowWildcards: true,
                    allowCommonName: false)) {
                throw new InvalidOperationException(
                    "Mail inbox SMTP TLS certificate does not match the configured server name.");
            }

            return certificate;
        } catch {
            certificate.Dispose();
            throw;
        }
    }
}
