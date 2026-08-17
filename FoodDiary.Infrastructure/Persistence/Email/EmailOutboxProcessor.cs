using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Infrastructure.Persistence.Outbox;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Persistence.Email;

internal sealed class EmailOutboxProcessor(
    FoodDiaryDbContext context,
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
