using Microsoft.Extensions.Options;
using SmtpServer;
using SmtpServer.Mail;
using SmtpServer.Storage;
using FoodDiary.MailInbox.Infrastructure.Options;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxMailboxFilter(IOptions<MailInboxSmtpOptions> options) : MailboxFilter {
    private readonly HashSet<string> _allowedRecipients = options.Value.AllowedRecipients
        .Select(static value => value.Trim().ToLowerInvariant())
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public override Task<bool> CanAcceptFromAsync(
        ISessionContext context,
        IMailbox from,
        int size,
        CancellationToken cancellationToken) {
        return Task.FromResult(true);
    }

    public override Task<bool> CanDeliverToAsync(
        ISessionContext context,
        IMailbox to,
        IMailbox from,
        CancellationToken cancellationToken) {
        var address = to.AsAddress().Trim().ToLowerInvariant();
        return Task.FromResult(_allowedRecipients.Contains(address));
    }
}
