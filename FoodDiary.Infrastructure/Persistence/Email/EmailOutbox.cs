using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Infrastructure.Persistence.Email;

internal sealed class EmailOutbox(
    FoodDiaryDbContext context,
    TimeProvider timeProvider) : IEmailOutbox {
    public async Task EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default) {
        var outboxMessage = EmailOutboxMessage.Create(
            message,
            timeProvider.GetUtcNow().UtcDateTime);

        await context.EmailOutbox.AddAsync(outboxMessage, cancellationToken).ConfigureAwait(false);
    }
}
