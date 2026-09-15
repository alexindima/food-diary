using FoodDiary.Persistence.Runtime.Persistence;
using FoodDiary.Infrastructure.Persistence.Email;
using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Email.Infrastructure.Persistence;

internal sealed class EmailOutbox(
    SharedPersistenceDbContext context,
    TimeProvider timeProvider) : IEmailOutbox {
    public async Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        var outboxMessage = EmailOutboxMessage.Create(
            message,
            timeProvider.GetUtcNow().UtcDateTime);

        await context.EmailOutbox.AddAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
    }
}
