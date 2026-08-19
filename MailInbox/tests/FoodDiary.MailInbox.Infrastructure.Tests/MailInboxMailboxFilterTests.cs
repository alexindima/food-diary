using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using SmtpServer;
using SmtpServer.IO;
using SmtpServer.Mail;
using SmtpServer.Net;

namespace FoodDiary.MailInbox.Infrastructure.Tests;

[ExcludeFromCodeCoverage]
public sealed class MailInboxMailboxFilterTests {
    [Fact]
    public async Task CanAcceptFromAsync_ReturnsTrue() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
        });

        bool canAccept = await filter.CanAcceptFromAsync(
            context: null!,
            from: new Mailbox("sender", "example.com"),
            size: 1024,
            cancellationToken: CancellationToken.None);

        Assert.True(canAccept);
    }

    [Fact]
    public async Task CanDeliverToAsync_WhenRecipientIsAllowed_ReturnsTrue() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
        });

        bool canDeliver = await filter.CanDeliverToAsync(
            context: null!,
            to: new Mailbox("admin", "fooddiary.club"),
            from: new Mailbox("sender", "example.com"),
            cancellationToken: CancellationToken.None);

        Assert.True(canDeliver);
    }

    [Fact]
    public async Task CanDeliverToAsync_WhenRecipientIsNotAllowed_ReturnsFalse() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
        });

        bool canDeliver = await filter.CanDeliverToAsync(
            context: null!,
            to: new Mailbox("unknown", "fooddiary.club"),
            from: new Mailbox("sender", "example.com"),
            cancellationToken: CancellationToken.None);

        Assert.False(canDeliver);
    }

    [Fact]
    public async Task CanAcceptFromAsync_WhenSessionMessageLimitIsExceeded_ReturnsFalse() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
            MaxMessagesPerSession = 1,
        });
        var context = new TestSessionContext();

        bool first = await filter.CanAcceptFromAsync(
            context,
            new Mailbox("sender", "example.com"),
            size: 100,
            CancellationToken.None);
        bool second = await filter.CanAcceptFromAsync(
            context,
            new Mailbox("sender", "example.com"),
            size: 100,
            CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task CanAcceptFromAsync_WhenDeclaredMessageSizeExceedsLimit_ReturnsFalse() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
            MaxMessageSizeBytes = 100,
        });

        bool canAccept = await filter.CanAcceptFromAsync(
            new TestSessionContext(),
            new Mailbox("sender", "example.com"),
            size: 101,
            CancellationToken.None);

        Assert.False(canAccept);
    }

    [Fact]
    public async Task CanDeliverToAsync_WhenRecipientLimitIsExceeded_ReturnsFalse() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club", "support@fooddiary.club"],
            MaxRecipientsPerMessage = 1,
        });
        var context = new TestSessionContext();
        await filter.CanAcceptFromAsync(
            context,
            new Mailbox("sender", "example.com"),
            size: 100,
            CancellationToken.None);

        bool first = await filter.CanDeliverToAsync(
            context,
            new Mailbox("admin", "fooddiary.club"),
            new Mailbox("sender", "example.com"),
            CancellationToken.None);
        bool second = await filter.CanDeliverToAsync(
            context,
            new Mailbox("support", "fooddiary.club"),
            new Mailbox("sender", "example.com"),
            CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task CanAcceptFromAsync_WhenSenderWindowIsExhausted_ReturnsFalse() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
            MaxMessagesPerSenderPerHour = 1,
        });

        bool first = await filter.CanAcceptFromAsync(
            new TestSessionContext(),
            new Mailbox("sender", "example.com"),
            size: 100,
            CancellationToken.None);
        bool second = await filter.CanAcceptFromAsync(
            new TestSessionContext(),
            new Mailbox("sender", "example.com"),
            size: 100,
            CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task CanAcceptFromAsync_WhenAnotherSourceSpoofsLimitedSender_DoesNotPoisonQuota() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
            MaxMessagesPerSenderPerHour = 1,
        });
        Mailbox sender = new("sender", "example.com");

        bool attacker = await filter.CanAcceptFromAsync(
            new TestSessionContext(IPAddress.Parse("192.0.2.10")),
            sender,
            size: 100,
            CancellationToken.None);
        bool legitimate = await filter.CanAcceptFromAsync(
            new TestSessionContext(IPAddress.Parse("198.51.100.20")),
            sender,
            size: 100,
            CancellationToken.None);

        Assert.True(attacker);
        Assert.True(legitimate);
    }

    [Fact]
    public async Task CanAcceptFromAsync_WhenIpWindowIsExhausted_ReturnsFalse() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
            MaxMessagesPerIpPerHour = 1,
        });
        var sourceAddress = IPAddress.Parse("192.0.2.10");

        bool first = await filter.CanAcceptFromAsync(
            new TestSessionContext(sourceAddress),
            new Mailbox("first", "example.com"),
            size: 100,
            CancellationToken.None);
        bool second = await filter.CanAcceptFromAsync(
            new TestSessionContext(sourceAddress),
            new Mailbox("second", "example.com"),
            size: 100,
            CancellationToken.None);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task CanAcceptFromAsync_WhenIpv6AddressRotatesWithinPrefix_SharesIpQuota() {
        MailInboxMailboxFilter filter = CreateFilter(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"],
            MaxMessagesPerIpPerHour = 1,
        });

        bool first = await filter.CanAcceptFromAsync(
            new TestSessionContext(IPAddress.Parse("2001:db8:1234:5678::1")),
            new Mailbox("first", "example.com"),
            size: 100,
            CancellationToken.None);
        bool rotated = await filter.CanAcceptFromAsync(
            new TestSessionContext(IPAddress.Parse("2001:db8:1234:5678::ffff")),
            new Mailbox("second", "example.com"),
            size: 100,
            CancellationToken.None);

        Assert.True(first);
        Assert.False(rotated);
    }

    [Fact]
    public void RateLimiter_WhenTrackedKeyCapacityIsReached_ShardsOverflowByIdentity() {
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> options =
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions {
                MaxTrackedRateLimitKeys = 2,
            });
        var limiter = new MailInboxFixedWindowRateLimiter(options, TimeProvider.System);

        Assert.True(limiter.TryAcquire("sender", "tracked", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("sender", "overflow-a", permitLimit: 2, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("sender", "overflow-a", permitLimit: 2, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("sender", "overflow-a", permitLimit: 2, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("sender", "overflow-b", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("sender", "tracked", permitLimit: 1, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void RateLimiter_WhenExistingWindowExpires_StartsNewWindow() {
        var timeProvider = new AdjustableTimeProvider();
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> options =
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions());
        var limiter = new MailInboxFixedWindowRateLimiter(options, timeProvider);
        Assert.True(limiter.TryAcquire("ip", "192.0.2.10", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("ip", "192.0.2.10", permitLimit: 1, TimeSpan.FromHours(1)));

        timeProvider.Advance(TimeSpan.FromHours(1));

        Assert.True(limiter.TryAcquire("ip", "192.0.2.10", permitLimit: 1, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void RateLimiter_WhenAddingNewKey_RemovesExpiredOtherWindows() {
        var timeProvider = new AdjustableTimeProvider();
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> options =
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions {
                MaxTrackedRateLimitKeys = 2,
            });
        var limiter = new MailInboxFixedWindowRateLimiter(options, timeProvider);
        Assert.True(limiter.TryAcquire("ip", "old", permitLimit: 1, TimeSpan.FromHours(1)));
        timeProvider.Advance(TimeSpan.FromHours(1));

        Assert.True(limiter.TryAcquire("ip", "new", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("ip", "new", permitLimit: 1, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void RateLimiter_WhenCapacityIsReached_KeepsOverflowBudgetsIndependentByScope() {
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> options =
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions {
                MaxTrackedRateLimitKeys = 1,
            });
        var limiter = new MailInboxFixedWindowRateLimiter(options, TimeProvider.System);
        Assert.True(limiter.TryAcquire("ip", "192.0.2.10", permitLimit: 1, TimeSpan.FromHours(1)));

        Assert.True(limiter.TryAcquire("sender", "sender@example.com", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("sender", "other@example.com", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("sender", "other@example.com", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("custom", "value", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("custom", "value", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.False(limiter.TryAcquire("ip", "192.0.2.10", permitLimit: 1, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void RateLimiter_WhenValuesCollideUnderPublicHash_UsesSecretOverflowAssignment() {
        byte[] secret = [.. Enumerable.Range(1, 32).Select(static value => (byte)value)];
        const string victim = "victim@example.com";
        string attacker = Enumerable.Range(0, 100_000)
            .Select(static value => FormattableString.Invariant($"attacker-{value}@example.com"))
            .First(value =>
                GetPublicShard("sender", value) == GetPublicShard("sender", victim) &&
                GetKeyedShard(secret, "sender", value) != GetKeyedShard(secret, "sender", victim));
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> options =
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions {
                MaxTrackedRateLimitKeys = 2,
            });
        var limiter = new MailInboxFixedWindowRateLimiter(options, TimeProvider.System, secret);

        Assert.True(limiter.TryAcquire("sender", "tracked@example.com", permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("sender", attacker, permitLimit: 1, TimeSpan.FromHours(1)));
        Assert.True(limiter.TryAcquire("sender", victim, permitLimit: 1, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void RateLimiter_WhenPermitsWouldExceedBudget_RejectsWithoutPartialCharge() {
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> options =
            Microsoft.Extensions.Options.Options.Create(new MailInboxSmtpOptions());
        var limiter = new MailInboxFixedWindowRateLimiter(options, TimeProvider.System);

        Assert.True(limiter.TryAcquire("ip-bytes", "192.0.2.10", 100, TimeSpan.FromHours(1), permits: 60));
        Assert.False(limiter.TryAcquire("ip-bytes", "192.0.2.10", 100, TimeSpan.FromHours(1), permits: 50));
        Assert.True(limiter.TryAcquire("ip-bytes", "192.0.2.10", 100, TimeSpan.FromHours(1), permits: 40));
        Assert.False(limiter.TryAcquire("ip-bytes", "192.0.2.10", 100, TimeSpan.FromHours(1), permits: 1));
    }

    private static MailInboxMailboxFilter CreateFilter(MailInboxSmtpOptions options) {
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> optionsWrapper =
            Microsoft.Extensions.Options.Options.Create(options);
        return new MailInboxMailboxFilter(
            optionsWrapper,
            new MailInboxFixedWindowRateLimiter(optionsWrapper, TimeProvider.System));
    }

    private static int GetPublicShard(string scope, string value) {
        byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes($"{scope}\n{value}"));
        return (key[0] << 4) | (key[1] >> 4);
    }

    private static int GetKeyedShard(byte[] secret, string scope, string value) {
        byte[] key = SHA256.HashData(Encoding.UTF8.GetBytes($"{scope}\n{value}"));
        byte[] hash = HMACSHA256.HashData(secret, Encoding.ASCII.GetBytes(Convert.ToHexString(key)));
        return (hash[0] << 4) | (hash[1] >> 4);
    }

    [ExcludeFromCodeCoverage]
    private sealed class AdjustableTimeProvider : TimeProvider {
        private DateTimeOffset _utcNow = new(2026, 8, 19, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow += duration;
    }

    [ExcludeFromCodeCoverage]
    private sealed class TestSessionContext(IPAddress? remoteAddress = null) : ISessionContext {
        public Guid SessionId { get; } = Guid.NewGuid();
        public IServiceProvider ServiceProvider => null!;
        public ISmtpServerOptions ServerOptions => null!;
        public IEndpointDefinition EndpointDefinition => null!;
        public ISecurableDuplexPipe Pipe => null!;
        public AuthenticationContext Authentication => null!;
        public IDictionary<string, object> Properties { get; } = CreateProperties(remoteAddress);

        public event EventHandler<SmtpCommandEventArgs>? CommandExecuting { add { } remove { } }
        public event EventHandler<SmtpCommandEventArgs>? CommandExecuted { add { } remove { } }
        public event EventHandler<SmtpResponseExceptionEventArgs>? ResponseException { add { } remove { } }
        public event EventHandler<EventArgs>? SessionAuthenticated { add { } remove { } }

        private static Dictionary<string, object> CreateProperties(IPAddress? remoteAddress) {
            var properties = new Dictionary<string, object>(StringComparer.Ordinal);
            if (remoteAddress is not null) {
                properties.Add(EndpointListener.RemoteEndPointKey, new IPEndPoint(remoteAddress, 2525));
            }

            return properties;
        }
    }
}
