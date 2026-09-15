using FoodDiary.Outbox.Infrastructure.Options;
using FoodDiary.Outbox.Infrastructure.Persistence;
using FoodDiary.Application.Abstractions.Email.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Persistence.Runtime.Persistence.Email;

internal sealed class EmailOutboxProcessor(
    SharedPersistenceDbContext context,
    IEmailTransport emailTransport,
    IOptions<OutboxProcessingOptions> options,
    TimeProvider timeProvider,
    ILogger<EmailOutboxProcessor> logger) : IEmailOutboxProcessor {
    public Task<int> ProcessDueAsync(int batchSize, CancellationToken cancellationToken = default) =>
        OutboxProcessingEngine.ProcessDueAsync(
            context,
            context.EmailOutbox,
            "\"EmailOutbox\"",
            "email",
            batchSize,
            options.Value,
            timeProvider,
            (message, token) => emailTransport.SendAsync(message.ToEmailMessage(), token),
            static message => message.Id,
            logger,
            cancellationToken: cancellationToken);
}
