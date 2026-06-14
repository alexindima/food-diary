using System.Net.Sockets;
using MailKit.Security;
using MimeKit;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public interface IDirectMxSmtpClient : IDisposable {
    string LocalDomain { get; set; }
    Task ConnectAsync(Socket socket, string host, int port, SecureSocketOptions options, CancellationToken cancellationToken);
    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
    Task DisconnectAsync(bool quit, CancellationToken cancellationToken);
}
