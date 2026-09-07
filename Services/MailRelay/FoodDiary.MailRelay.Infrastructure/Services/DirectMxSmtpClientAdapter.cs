using System.Net.Sockets;
using System.Diagnostics.CodeAnalysis;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FoodDiary.MailRelay.Infrastructure.Services;

[ExcludeFromCodeCoverage]
public sealed class DirectMxSmtpClientAdapter : IDirectMxSmtpClient {
    private readonly SmtpClient _client = new();

    public string LocalDomain {
        get => _client.LocalDomain ?? string.Empty;
        set => _client.LocalDomain = value;
    }

    public Task ConnectAsync(
        Socket socket,
        string host,
        int port,
        SecureSocketOptions options,
        CancellationToken cancellationToken) =>
        _client.ConnectAsync(socket, host, port, options, cancellationToken);

    public Task SendAsync(MimeMessage message, CancellationToken cancellationToken) =>
        _client.SendAsync(message, cancellationToken);

    public Task DisconnectAsync(bool quit, CancellationToken cancellationToken) =>
        _client.DisconnectAsync(quit, cancellationToken);

    public void Dispose() => _client.Dispose();
}
