using System.Buffers;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;

namespace FoodDiary.MailInbox.Infrastructure.Services;

// SmtpServer disposes each transaction's store. The shared store and its global
// processing limiter belong to DI and must survive individual SMTP transactions.
internal sealed class SmtpMessageStoreAdapter(SmtpInboundMessageStore store) : MessageStore {
    public override Task<SmtpResponse> SaveAsync(
        ISessionContext context,
        IMessageTransaction transaction,
        ReadOnlySequence<byte> buffer,
        CancellationToken cancellationToken) =>
        store.SaveAsync(context, transaction, buffer, cancellationToken);
}
