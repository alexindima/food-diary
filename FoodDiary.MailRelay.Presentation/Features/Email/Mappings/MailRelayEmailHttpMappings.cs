using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Presentation.Controllers;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Presentation.Features.Email.Responses;

namespace FoodDiary.MailRelay.Presentation.Features.Email.Mappings;

public static class MailRelayEmailHttpMappings {
    public static GetMailRelayQueueStatsQuery ToQueueStatsQuery() => new();

    public static GetMailRelayMessageDetailsQuery ToMessageDetailsQuery(this Guid id) => new(id);

    public static GetMailRelaySuppressionsQuery ToSuppressionsQuery(this string? email) => new(email);

    public static GetMailRelayDeliveryEventsQuery ToDeliveryEventsQuery(this string? email) => new(email);

    public static EnqueueMailRelayEmailCommand ToCommand(this EnqueueMailRelayEmailRequest request) =>
        new(request.ToApplicationRequest());

    public static CreateMailRelaySuppressionCommand ToCommand(this CreateMailRelaySuppressionHttpRequest request) =>
        new(new CreateSuppressionRequest(
            request.Email,
            request.Reason,
            request.Source,
            request.ExpiresAtUtc));

    public static RemoveMailRelaySuppressionCommand ToRemoveSuppressionCommand(this string email) => new(email);

    public static IngestMailRelayDeliveryEventCommand ToCommand(this IngestMailRelayDeliveryEventHttpRequest request) =>
        new(request.ToApplicationRequest());

    public static IngestMailRelayDeliveryEventCommand ToCommand(this IngestMailEventRequest request) =>
        new(request);

    public static IngestManyMailRelayDeliveryEventsCommand ToCommand(this IReadOnlyList<IngestMailEventRequest> requests) =>
        new(requests);

    public static MailRelayMappedRequest<IReadOnlyList<MailRelayDeliveryEventEntry>> ToMappedCommand(
        this AwsSesSnsWebhookHttpRequest request) =>
        request.TryMapToDeliveryEvents(out var events, out var error)
            ? MailRelayMappedRequest<IReadOnlyList<MailRelayDeliveryEventEntry>>.Success(events.ToCommand())
            : MailRelayMappedRequest<IReadOnlyList<MailRelayDeliveryEventEntry>>.Failure(error);

    public static MailRelayMappedRequest<MailRelayDeliveryEventEntry> ToMappedCommand(
        this MailgunWebhookHttpRequest request) =>
        request.TryMapToDeliveryEvent(out var deliveryEvent, out var error) && deliveryEvent is not null
            ? MailRelayMappedRequest<MailRelayDeliveryEventEntry>.Success(deliveryEvent.ToCommand())
            : MailRelayMappedRequest<MailRelayDeliveryEventEntry>.Failure(error);

    public static RelayEmailMessageRequest ToApplicationRequest(this EnqueueMailRelayEmailRequest request) =>
        new(
            request.FromAddress,
            request.FromName,
            request.To,
            request.Subject,
            request.HtmlBody,
            request.TextBody,
            request.CorrelationId,
            request.IdempotencyKey);

    public static IngestMailEventRequest ToApplicationRequest(this IngestMailRelayDeliveryEventHttpRequest request) =>
        new(
            request.EventType,
            request.Email,
            request.Source,
            request.Classification,
            request.ProviderMessageId,
            request.Reason,
            request.OccurredAtUtc);

    public static EnqueueMailRelayEmailResponse ToEnqueuedHttpResponse(this Guid id) =>
        new(id, "queued");

    public static MailRelaySuppressionCreatedHttpResponse ToSuppressionCreatedHttpResponse() =>
        new("suppressed");

    public static MailRelayQueueStatsHttpResponse ToHttpResponse(this MailRelayQueueStats stats) =>
        new(
            stats.PendingCount,
            stats.RetryCount,
            stats.ProcessingCount,
            stats.SentCount,
            stats.FailedCount,
            stats.SuppressedCount);

    public static MailRelayMessageDetailsHttpResponse ToHttpResponse(this MailRelayMessageDetails message) =>
        new(
            message.Id,
            message.Status,
            message.Subject,
            message.CorrelationId,
            message.AttemptCount,
            message.MaxAttempts,
            message.CreatedAtUtc,
            message.AvailableAtUtc,
            message.LockedAtUtc,
            message.SentAtUtc,
            message.LastError,
            message.SuppressedRecipients);

    public static MailRelaySuppressionHttpResponse ToHttpResponse(this MailRelaySuppressionEntry suppression) =>
        new(
            suppression.Email,
            suppression.Reason,
            suppression.Source,
            suppression.CreatedAtUtc,
            suppression.UpdatedAtUtc,
            suppression.ExpiresAtUtc);

    public static IReadOnlyList<MailRelaySuppressionHttpResponse> ToHttpResponse(
        this IReadOnlyList<MailRelaySuppressionEntry> suppressions) =>
        suppressions.Select(static suppression => suppression.ToHttpResponse()).ToList();

    public static MailRelayDeliveryEventHttpResponse ToHttpResponse(this MailRelayDeliveryEventEntry deliveryEvent) =>
        new(
            deliveryEvent.Id,
            deliveryEvent.EventType,
            deliveryEvent.Email,
            deliveryEvent.Source,
            deliveryEvent.Classification,
            deliveryEvent.ProviderMessageId,
            deliveryEvent.Reason,
            deliveryEvent.OccurredAtUtc,
            deliveryEvent.CreatedAtUtc);

    public static IReadOnlyList<MailRelayDeliveryEventHttpResponse> ToHttpResponse(
        this IReadOnlyList<MailRelayDeliveryEventEntry> deliveryEvents) =>
        deliveryEvents.Select(static deliveryEvent => deliveryEvent.ToHttpResponse()).ToList();

    public static MailRelayProviderIngestionHttpResponse ToProviderIngestionHttpResponse(
        this IReadOnlyList<MailRelayDeliveryEventEntry> deliveryEvents) =>
        new(deliveryEvents.Count);
}
