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
                await PurgeAsync(stoppingToken).ConfigureAwait(false);
            } catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
                return;
            } catch (Exception exception) {
                MailInboxTelemetry.RecordRetention("failure", 1);
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

        MailInboxTelemetry.RecordRetention("content_purged", result.ContentPurgedCount);
        MailInboxTelemetry.RecordRetention("metadata_deleted", result.MetadataDeletedCount);
        if (result.ContentPurgedCount > 0 || result.MetadataDeletedCount > 0) {
            logger.LogInformation(
                "MailInbox retention completed. ContentPurged={ContentPurged}; MetadataDeleted={MetadataDeleted}",
                result.ContentPurgedCount,
                result.MetadataDeletedCount);
        }

        return result;
    }
}
