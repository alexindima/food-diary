using System.IO.Pipelines;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FoodDiary.MailInbox.Infrastructure.Services;
using SmtpServer;
using SmtpServer.IO;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxEndpointListenerFactoryTests {
    [Fact]
    public void GetSourceKey_WhenRemoteEndpointIsMissing_ReturnsUnknown() {
        Type listenerType = GetNestedType("LimitedEndpointListener");
        MethodInfo method = listenerType.GetMethod(
            "GetSourceKey",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        string result = (string)method.Invoke(obj: null, [new TestSessionContext()])!;

        Assert.Equal("unknown", result);
    }

    [Fact]
    public async Task LimitedDuplexPipe_ForwardsSecurityMembersAndReleasesOnlyOnce() {
        var inner = new RecordingSecurableDuplexPipe();
        int releaseCount = 0;
        Type pipeType = GetNestedType("LimitedDuplexPipe");
        ConstructorInfo constructor = pipeType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        var pipe = (ISecurableDuplexPipe)constructor.Invoke([inner, (Action)(() => releaseCount++)]);
        using X509Certificate2 certificate = CreateCertificate();

        await pipe.UpgradeAsync(certificate, SslProtocols.Tls12, CancellationToken.None);
        PipeReader input = pipe.Input;
        PipeWriter output = pipe.Output;
        bool isSecure = pipe.IsSecure;
        SslProtocols protocol = pipe.SslProtocol;
        pipe.Dispose();
        pipe.Dispose();

        Assert.Same(inner.Input, input);
        Assert.Same(inner.Output, output);
        Assert.True(isSecure);
        Assert.Equal(SslProtocols.Tls13, protocol);
        Assert.True(inner.UpgradeCalled);
        Assert.True(inner.Disposed);
        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public void ConnectionLimiter_ReleaseSource_CoversDecrementRemovalAndMissingSource() {
        Type limiterType = GetNestedType("ConnectionLimiter");
        ConstructorInfo constructor = limiterType.GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Single();
        using var limiter = (IDisposable)constructor.Invoke([2, 2]);
        MethodInfo acquire = limiterType.GetMethod("TryAcquireSource")!;
        MethodInfo release = limiterType.GetMethod("ReleaseSource", BindingFlags.Instance | BindingFlags.NonPublic)!;

        Assert.True((bool)acquire.Invoke(limiter, ["source"])!);
        Assert.True((bool)acquire.Invoke(limiter, ["source"])!);
        release.Invoke(limiter, ["source"]);
        release.Invoke(limiter, ["source"]);
        release.Invoke(limiter, ["missing"]);

        Assert.True((bool)acquire.Invoke(limiter, ["source"])!);
    }

    private static Type GetNestedType(string name) => typeof(MailInboxEndpointListenerFactory).GetNestedType(
        name,
        BindingFlags.NonPublic) ?? throw new InvalidOperationException($"Nested type {name} was not found.");

    private static X509Certificate2 CreateCertificate() {
        using var rsa = RSA.Create();
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingSecurableDuplexPipe : ISecurableDuplexPipe {
        private readonly Pipe _input = new();
        private readonly Pipe _output = new();

        public PipeReader Input => _input.Reader;

        public PipeWriter Output => _output.Writer;

        public bool IsSecure => true;

        public SslProtocols SslProtocol => SslProtocols.Tls13;

        public bool UpgradeCalled { get; private set; }

        public bool Disposed { get; private set; }

        public Task UpgradeAsync(
            X509Certificate certificate,
            SslProtocols protocols,
            CancellationToken cancellationToken = default) {
            UpgradeCalled = true;
            return Task.CompletedTask;
        }

        public void Dispose() {
            Disposed = true;
            _input.Reader.Complete();
            _input.Writer.Complete();
            _output.Reader.Complete();
            _output.Writer.Complete();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestSessionContext : ISessionContext {
        public Guid SessionId { get; } = Guid.NewGuid();
        public IServiceProvider ServiceProvider => null!;
        public ISmtpServerOptions ServerOptions => null!;
        public IEndpointDefinition EndpointDefinition => null!;
        public ISecurableDuplexPipe Pipe => null!;
        public AuthenticationContext Authentication => null!;
        public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>(StringComparer.Ordinal);

        public event EventHandler<SmtpCommandEventArgs>? CommandExecuting { add { } remove { } }
        public event EventHandler<SmtpCommandEventArgs>? CommandExecuted { add { } remove { } }
        public event EventHandler<SmtpResponseExceptionEventArgs>? ResponseException { add { } remove { } }
        public event EventHandler<EventArgs>? SessionAuthenticated { add { } remove { } }
    }
}
