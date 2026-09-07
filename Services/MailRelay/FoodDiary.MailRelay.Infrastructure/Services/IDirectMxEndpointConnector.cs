using System.Net.Sockets;

namespace FoodDiary.MailRelay.Infrastructure.Services;

public interface IDirectMxEndpointConnector {
    Task<Socket> ConnectAsync(string mxHost, int port, CancellationToken cancellationToken);
}
