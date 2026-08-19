using FoodDiary.MailInbox.Application.Abstractions;
using FoodDiary.MailInbox.Application.Messages.Models;
using FoodDiary.MailInbox.Application.Telemetry;
using FoodDiary.MailInbox.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public sealed class MailInboxRetentionHostedService(
    IInboundMailStore store,
    IOptions<MailInboxStorageOptions> options,
    TimeProvider timeProvider,
    ILogger<MailInboxRetentionHostedService> logger) : BackgroundService {
    private readonly MailInboxStorageOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            try {
                await DrainExpiredAsync(stoppingToken).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                MailInboxTelemetry.RecordRetention(MailInboxRetentionOutcome.Failure, 1);
                logger.LogError(
                    "MailInbox retention failed. ErrorType={ErrorType}",
                    exception.GetType().Name);
            }

            try {
                await Task.Delay(_options.CleanupInterval, timeProvider, stoppingToken).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            }
        }
    }

    internal async Task<InboundMailRetentionResult> PurgeAsync(CancellationToken cancellationToken) {
        DateTimeOffset now = timeProvider.GetUtcNow();
        InboundMailRetentionResult result = await store.PurgeExpiredAsync(
            now - _options.ContentRetention,
            now - _options.MetadataRetention,
            _options.CleanupBatchSize,
            cancellationToken).ConfigureAwait(false);

        MailInboxTelemetry.RecordRetention(MailInboxRetentionOutcome.ContentPurged, result.ContentPurgedCount);
        MailInboxTelemetry.RecordRetention(MailInboxRetentionOutcome.MetadataDeleted, result.MetadataDeletedCount);
        if (result.ContentPurgedCount > 0 || result.MetadataDeletedCount > 0) {
            logger.LogInformation(
                "MailInbox retention completed. ContentPurged={ContentPurged}; MetadataDeleted={MetadataDeleted}",
                result.ContentPurgedCount,
                result.MetadataDeletedCount);
        }

        return result;
    }

    internal async Task<InboundMailRetentionResult> DrainExpiredAsync(CancellationToken cancellationToken) {
        int totalContentPurged = 0;
        int totalMetadataDeleted = 0;
        InboundMailRetentionResult batch;
        do {
            batch = await PurgeAsync(cancellationToken).ConfigureAwait(false);
            totalContentPurged = checked(totalContentPurged + batch.ContentPurgedCount);
            totalMetadataDeleted = checked(totalMetadataDeleted + batch.MetadataDeletedCount);
            if (batch.ContentPurgedCount == _options.CleanupBatchSize ||
                batch.MetadataDeletedCount == _options.CleanupBatchSize) {
                await Task.Yield();
            }
        } while (batch.ContentPurgedCount == _options.CleanupBatchSize ||
                 batch.MetadataDeletedCount == _options.CleanupBatchSize);

        return new InboundMailRetentionResult(totalContentPurged, totalMetadataDeleted);
    }
}
