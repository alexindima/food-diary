using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using Docker.DotNet.Models;
using Testcontainers.Redis;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal sealed class SignalRRedisServer : IAsyncDisposable {
    private readonly RedisContainer? _container;
    private readonly Process? _process;

    private SignalRRedisServer(string connectionString, RedisContainer? container = null, Process? process = null) {
        ConnectionString = connectionString;
        _container = container;
        _process = process;
    }

    public string ConnectionString { get; }

    public static string? NativeExecutable => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Redis", "redis-server.exe")
        : null;

    public static async Task<SignalRRedisServer> StartAsync(CancellationToken cancellationToken) {
        if (NativeExecutable is string executable && File.Exists(executable)) {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            string portText = port.ToString(CultureInfo.InvariantCulture);
            listener.Stop();
            var start = new ProcessStartInfo(executable) {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetTempPath(),
            };
            foreach (string argument in new[] { "--bind", "127.0.0.1", "--port", portText, "--save", "", "--appendonly", "no", "--maxmemory", "32mb" }) {
                start.ArgumentList.Add(argument);
            }
            Process process = Process.Start(start) ?? throw new InvalidOperationException("Unable to start test-owned Redis.");
            var server = new SignalRRedisServer($"127.0.0.1:{portText},abortConnect=false", process: process);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try {
                using var startup = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                startup.CancelAfter(TimeSpan.FromSeconds(10));
                while (true) {
                    if (process.HasExited) {
                        throw new InvalidOperationException($"Test-owned Redis exited with code {process.ExitCode.ToString(CultureInfo.InvariantCulture)}.");
                    }
                    try {
                        using var probe = new TcpClient();
                        await probe.ConnectAsync(IPAddress.Loopback, port, startup.Token);
                        return server;
                    } catch (SocketException) {
                        // Poll only the external listener, with a bounded startup deadline.
                        await Task.Delay(TimeSpan.FromMilliseconds(50), startup.Token);
                    }
                }
            } catch {
                await server.DisposeAsync();
                throw;
            }
        }

        RedisContainer container = new RedisBuilder("redis:7-alpine")
            .WithCreateParameterModifier(parameters => {
                parameters.HostConfig ??= new HostConfig();
                parameters.HostConfig.PortBindings ??= new Dictionary<string, IList<PortBinding>>(StringComparer.Ordinal);
                parameters.HostConfig.PortBindings["6379/tcp"] = [new PortBinding { HostIP = IPAddress.Loopback.ToString(), HostPort = "0" }];
            }).Build();
        try {
            await container.StartAsync(cancellationToken);
            return new SignalRRedisServer(container.GetConnectionString(), container);
        } catch {
            await container.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync() {
        if (_container is not null) {
            await _container.DisposeAsync();
        }
        if (_process is not null) {
            if (!_process.HasExited) {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            _process.Dispose();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    [ExcludeFromCodeCoverage]
    internal sealed class RequiresRedisTheoryAttribute : TheoryAttribute {
        public RequiresRedisTheoryAttribute() {
            if (NativeExecutable is string executable && File.Exists(executable)) {
                return;
            }
            if (!DockerAvailability.IsAvailable(out string? reason)) {
                Skip = reason;
            }
        }
    }
}
