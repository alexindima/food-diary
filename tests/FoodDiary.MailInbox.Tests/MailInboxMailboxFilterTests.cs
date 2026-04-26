using FoodDiary.MailInbox.Infrastructure.Options;
using FoodDiary.MailInbox.Infrastructure.Services;
using Microsoft.Extensions.Options;
using SmtpServer.Mail;

namespace FoodDiary.MailInbox.Tests;

public sealed class MailInboxMailboxFilterTests {
    [Fact]
    public async Task CanDeliverToAsync_WhenRecipientIsAllowed_ReturnsTrue() {
        var filter = new MailInboxMailboxFilter(Options.Create(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"]
        }));

        var canDeliver = await filter.CanDeliverToAsync(
            context: null!,
            from: new Mailbox("sender", "example.com"),
            to: new Mailbox("admin", "fooddiary.club"),
            cancellationToken: CancellationToken.None);

        Assert.True(canDeliver);
    }

    [Fact]
    public async Task CanDeliverToAsync_WhenRecipientIsNotAllowed_ReturnsFalse() {
        var filter = new MailInboxMailboxFilter(Options.Create(new MailInboxSmtpOptions {
            AllowedRecipients = ["admin@fooddiary.club"]
        }));

        var canDeliver = await filter.CanDeliverToAsync(
            context: null!,
            from: new Mailbox("sender", "example.com"),
            to: new Mailbox("unknown", "fooddiary.club"),
            cancellationToken: CancellationToken.None);

        Assert.False(canDeliver);
    }
}
