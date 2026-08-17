using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.IO;
using SmtpServer.Net;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxEndpointListenerFactory(IOptions<MailInboxSmtpOptions> options)
    : IEndpointListenerFactory, IDisposable {
    private readonly EndpointListenerFactory _innerFactory = new();
    private readonly ConnectionLimiter _limiter = new(
        options.Value.MaxConcurrentConnections,
        options.Value.MaxConcurrentConnectionsPerIp);

    public IEndpointListener CreateListener(IEndpointDefinition endpointDefinition) =>
        new LimitedEndpointListener(_innerFactory.CreateListener(endpointDefinition), _limiter);

    public void Dispose() => _limiter.Dispose();

    private sealed class LimitedEndpointListener(
        IEndpointListener inner,
        ConnectionLimiter limiter) : IEndpointListener {
        public async Task<ISecurableDuplexPipe> GetPipeAsync(
            ISessionContext context,
            CancellationToken cancellationToken) {
            await limiter.WaitForGlobalSlotAsync(cancellationToken).ConfigureAwait(false);
            ISecurableDuplexPipe? pipe = null;
            try {
                pipe = await inner.GetPipeAsync(context, cancellationToken).ConfigureAwait(false);
                string sourceKey = GetSourceKey(context);
                if (!limiter.TryAcquireSource(sourceKey)) {
                    pipe.Dispose();
                    pipe = null;
                    throw new InvalidOperationException("MailInbox per-source SMTP connection limit exceeded.");
                }

                return new LimitedDuplexPipe(pipe, () => limiter.Release(sourceKey));
            } catch {
                pipe?.Dispose();
                limiter.ReleaseGlobal();
                throw;
            }
        }

        public void Dispose() => inner.Dispose();

        private static string GetSourceKey(ISessionContext context) {
            if (context.Properties.TryGetValue(EndpointListener.RemoteEndPointKey, out object? value) &&
                value is IPEndPoint endpoint) {
                return Convert.ToHexString(SHA256.HashData(endpoint.Address.GetAddressBytes()));
            }

            return "unknown";
        }
    }

    private sealed class ConnectionLimiter(int globalLimit, int perSourceLimit) : IDisposable {
        private readonly SemaphoreSlim _global = new(globalLimit, globalLimit);
        private readonly ConcurrentDictionary<string, int> _sourceCounts = new(StringComparer.Ordinal);

        public Task WaitForGlobalSlotAsync(CancellationToken cancellationToken) =>
            _global.WaitAsync(cancellationToken);

        public bool TryAcquireSource(string sourceKey) {
            int count = _sourceCounts.AddOrUpdate(sourceKey, 1, static (_, current) => current + 1);
            if (count <= perSourceLimit) {
                return true;
            }

            ReleaseSource(sourceKey);
            return false;
        }

        public void Release(string sourceKey) {
            ReleaseSource(sourceKey);
            ReleaseGlobal();
        }

        public void ReleaseGlobal() => _global.Release();

        public void Dispose() => _global.Dispose();

        private void ReleaseSource(string sourceKey) {
            while (_sourceCounts.TryGetValue(sourceKey, out int current)) {
                if (current <= 1) {
                    if (_sourceCounts.TryRemove(new KeyValuePair<string, int>(sourceKey, current))) {
                        return;
                    }

                    continue;
                }

                if (_sourceCounts.TryUpdate(sourceKey, current - 1, current)) {
                    return;
                }
            }
        }
    }

    private sealed class LimitedDuplexPipe(
        ISecurableDuplexPipe inner,
        Action release) : ISecurableDuplexPipe {
        private int _disposed;

        public PipeReader Input => inner.Input;

        public PipeWriter Output => inner.Output;

        public bool IsSecure => inner.IsSecure;

        public SslProtocols SslProtocol => inner.SslProtocol;

        public Task UpgradeAsync(
            System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            SslProtocols protocols,
            CancellationToken cancellationToken = default) =>
            inner.UpgradeAsync(certificate, protocols, cancellationToken);

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }

            try {
                inner.Dispose();
            } finally {
                release();
            }
        }
    }
}
