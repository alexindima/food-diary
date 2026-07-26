using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Infrastructure.Persistence.Email;

internal sealed class EmailOutboxProcessor(
    FoodDiaryDbContext context,
    IEmailTransport emailTransport,
    TimeProvider timeProvider,
    ILogger<EmailOutboxProcessor> logger) : IEmailOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            context.EmailOutbox,
            "\"EmailOutbox\"",
            "email",
            batchSize,
            timeProvider,
            (message, token) => emailTransport.SendAsync(message.ToEmailMessage(), token),
            static message => message.Id,
            logger,
            cancellationToken: cancellationToken);
}
