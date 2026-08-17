using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using System.Net;
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

    private static MailInboxMailboxFilter CreateFilter(MailInboxSmtpOptions options) {
        Microsoft.Extensions.Options.IOptions<MailInboxSmtpOptions> optionsWrapper =
            Microsoft.Extensions.Options.Options.Create(options);
        return new MailInboxMailboxFilter(
            optionsWrapper,
            new MailInboxFixedWindowRateLimiter(optionsWrapper, TimeProvider.System));
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
