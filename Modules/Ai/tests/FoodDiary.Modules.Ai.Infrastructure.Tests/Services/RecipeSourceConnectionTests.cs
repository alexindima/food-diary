using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

namespace FoodDiary.Modules.Ai.Infrastructure.Tests.Services;

[ExcludeFromCodeCoverage]
public sealed class RecipeSourceConnectionTests {
    private const string IsolatedNetworkMarker = "FOODDIARY_TEST_ISOLATED_NETWORK";

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("localhost")]
    public async Task ResolvedPrivateAddress_IsRejectedBeforeOpeningTheOwnedSocketAsync(string host) {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        try {
            using HttpClient client = CreateClient();
            using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var target = new UriBuilder("http", host, Port(listener));

            HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync(target.Uri, harness.Token));

            Assert.Contains("Recipe source is not a public network destination.", error.ToString(), StringComparison.Ordinal);
            Assert.False(listener.Pending());
            Assert.False(harness.IsCancellationRequested);
        } finally {
            listener.Stop();
        }
    }

    [IsolatedPublicSocketFact]
    [Trait("Category", "Integration")]
    public async Task PublicAddress_ConnectsToTheOwnedServerAndReturnsItsExactResponseAsync() {
        IPAddress address = OwnedPublicAddress();
        var listener = new TcpListener(address, 0);
        listener.Start();
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try {
            using HttpClient client = CreateClient();
            Task server = RespondOnceAsync(listener, harness.Token);
            Task<string> response = client.GetStringAsync(new UriBuilder("http", address.ToString(), Port(listener)).Uri, harness.Token);

            await Task.WhenAll(server, response).WaitAsync(TimeSpan.FromSeconds(12));

            Assert.Equal("owned-socket", await response);
            Assert.False(harness.IsCancellationRequested);
        } finally {
            await harness.CancelAsync();
            listener.Stop();
        }
    }

    [IsolatedPublicSocketFact]
    [Trait("Category", "Integration")]
    public async Task RefusedPublicSocket_ReturnsTheControlledConnectionFailureAsync() {
        IPAddress address = OwnedPublicAddress();
        var reservation = new TcpListener(address, 0);
        reservation.Start();
        int closedPort = Port(reservation);
        reservation.Stop();
        using HttpClient client = CreateClient();
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        HttpRequestException error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetAsync(new UriBuilder("http", address.ToString(), closedPort).Uri, harness.Token));

        Assert.Contains("Recipe source connection failed.", error.ToString(), StringComparison.Ordinal);
        Assert.False(harness.IsCancellationRequested);
    }

    [IsolatedPublicSocketFact]
    [Trait("Category", "Integration")]
    public async Task CancellationOfAPendingPublicConnect_PropagatesWithoutReturningAStreamAsync() {
        IPAddress address = OwnedPublicAddress();
        var listener = new TcpListener(address, 0);
        listener.Start(backlog: 1);
        var heldConnections = new List<TcpClient>();
        using var harness = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var caller = CancellationTokenSource.CreateLinkedTokenSource(harness.Token);
        try {
            await SaturateAcceptQueueAsync(listener, address, heldConnections, harness.Token);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            int streamReturned = 0;
            using HttpClient client = CreateClient(async (context, token) => {
                using var connectionCancellation = CancellationTokenSource.CreateLinkedTokenSource(token, caller.Token);
                started.TrySetResult();
                try {
                    Stream stream = await RecipeSourceReader.ConnectPublicAsync(context, connectionCancellation.Token).ConfigureAwait(false);
                    Interlocked.Exchange(ref streamReturned, 1);
                    return stream;
                } finally {
                    finished.TrySetResult();
                }
            });
            Task<HttpResponseMessage> request = client.GetAsync(new UriBuilder("http", address.ToString(), Port(listener)).Uri, caller.Token);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(3), harness.Token);
            caller.CancelAfter(TimeSpan.FromMilliseconds(200));

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request.WaitAsync(TimeSpan.FromSeconds(3), harness.Token));
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(3), harness.Token);

            Assert.True(caller.IsCancellationRequested);
            Assert.False(harness.IsCancellationRequested);
            Assert.Equal(0, Volatile.Read(ref streamReturned));
            Assert.NotEmpty(heldConnections);
        } finally {
            await caller.CancelAsync();
            listener.Stop();
            foreach (TcpClient connection in heldConnections) { connection.Dispose(); }
        }
    }

    private static async Task SaturateAcceptQueueAsync(TcpListener listener, IPAddress address, List<TcpClient> held, CancellationToken cancellationToken) {
        for (int attempt = 0; attempt < 8; attempt++) {
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromMilliseconds(150));
            var connection = new TcpClient(AddressFamily.InterNetwork);
            bool retained = false;
            try {
                await connection.ConnectAsync(address, Port(listener), deadline.Token).ConfigureAwait(false);
                held.Add(connection);
                retained = true;
            } catch (OperationCanceledException) when (deadline.IsCancellationRequested && !cancellationToken.IsCancellationRequested) {
                Assert.NotEmpty(held);
                return;
            } finally {
                if (!retained) { connection.Dispose(); }
            }
        }
        throw new InvalidOperationException("The owned listener's bounded accept queue did not become full.");
    }

    private static async Task RespondOnceAsync(TcpListener listener, CancellationToken cancellationToken) {
        using TcpClient connection = await listener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
        await using NetworkStream stream = connection.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true);
        int lines = 0;
        while (true) {
            string? line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) { throw new IOException("The owned client closed before completing its request headers."); }
            if (line.Length == 0) { break; }
            if (++lines > 32) { throw new InvalidDataException("The owned fixture received too many request headers."); }
        }
        byte[] response = Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 12\r\nConnection: close\r\n\r\nowned-socket");
        await stream.WriteAsync(response, cancellationToken).ConfigureAwait(false);
    }

    private static int Port(TcpListener listener) => ((IPEndPoint)listener.LocalEndpoint).Port;

    private static HttpClient CreateClient(Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>>? connector = null) {
        var handler = new SocketsHttpHandler {
            UseProxy = false,
            AllowAutoRedirect = false,
            ConnectCallback = RecipeSourceReader.ConnectPublicAsync,
        };
        if (connector is not null) { handler.ConnectCallback = connector; }
        return new HttpClient(handler, disposeHandler: true);
    }

    private static IPAddress OwnedPublicAddress() {
        Assert.True(OperatingSystem.IsLinux());
        Assert.Equal("1", Environment.GetEnvironmentVariable(IsolatedNetworkMarker));
        Assert.StartsWith("fd-cov-", Environment.MachineName, StringComparison.Ordinal);
        NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
        Assert.NotEmpty(interfaces);
        Assert.All(interfaces, network => Assert.Equal(NetworkInterfaceType.Loopback, network.NetworkInterfaceType));
        var address = IPAddress.Parse("4.2.2.2");
        Assert.Contains(interfaces.SelectMany(network => network.GetIPProperties().UnicastAddresses), item => item.Address.Equals(address));
        Assert.True(RecipeSourceReader.IsPublicAddress(address));
        return address;
    }

    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Method)]
    private sealed class IsolatedPublicSocketFactAttribute : FactAttribute {
        public IsolatedPublicSocketFactAttribute() {
            if (!OperatingSystem.IsLinux() || !string.Equals(Environment.GetEnvironmentVariable(IsolatedNetworkMarker), "1", StringComparison.Ordinal)) {
                Skip = "Requires the owned network-none Linux coverage container with its public-formatted loopback alias.";
            }
        }
    }
}
