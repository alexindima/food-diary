using FoodDiary.MailRelay.Application.Emails.Commands;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Emails.Queries;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayEmailHttpMappingsTests {
    [Fact]
    public void StaticQueryMappings_CreateExpectedQueries() {
        var messageId = Guid.NewGuid();

        Assert.IsType<GetMailRelayQueueStatsQuery>(MailRelayEmailHttpMappings.ToQueueStatsQuery());
        Assert.Equal(messageId, messageId.ToMessageDetailsQuery().Id);
        Assert.Equal("user@example.com", "user@example.com".ToSuppressionsQuery().Email);
        Assert.Equal("user@example.com", "user@example.com".ToDeliveryEventsQuery().Email);
    }

    [Fact]
    public void EnqueueMailRelayEmailRequest_ToCommand_MapsApplicationRequest() {
        var request = new EnqueueMailRelayEmailRequest(
            FromAddress: "sender@example.com",
            FromName: "Sender",
            To: ["recipient@example.com"],
            Subject: "Subject",
            HtmlBody: "<p>Hello</p>",
            TextBody: "Hello",
            CorrelationId: "corr-1",
            IdempotencyKey: "idem-1");

        var command = request.ToCommand();

        Assert.Equal(request.FromAddress, command.Request.FromAddress);
        Assert.Equal(request.FromName, command.Request.FromName);
        Assert.Equal(request.To, command.Request.To);
        Assert.Equal(request.Subject, command.Request.Subject);
        Assert.Equal(request.HtmlBody, command.Request.HtmlBody);
        Assert.Equal(request.TextBody, command.Request.TextBody);
        Assert.Equal(request.CorrelationId, command.Request.CorrelationId);
        Assert.Equal(request.IdempotencyKey, command.Request.IdempotencyKey);
    }

    [Fact]
    public void CreateMailRelaySuppressionHttpRequest_ToCommand_MapsApplicationRequest() {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(7);
        var request = new CreateMailRelaySuppressionHttpRequest(
            Email: "blocked@example.com",
            Reason: "bounce",
            Source: "admin",
            ExpiresAtUtc: expiresAt);

        var command = request.ToCommand();

        Assert.Equal(request.Email, command.Request.Email);
        Assert.Equal(request.Reason, command.Request.Reason);
        Assert.Equal(request.Source, command.Request.Source);
        Assert.Equal(expiresAt, command.Request.ExpiresAtUtc);
    }

    [Fact]
    public void IngestMailRelayDeliveryEventHttpRequest_ToCommand_MapsApplicationRequest() {
        var occurredAt = DateTimeOffset.UtcNow;
        var request = new IngestMailRelayDeliveryEventHttpRequest(
            EventType: "bounce",
            Email: "recipient@example.com",
            Source: "manual",
            Classification: "hard",
            ProviderMessageId: "provider-1",
            Reason: "Mailbox unavailable",
            OccurredAtUtc: occurredAt);

        var command = request.ToCommand();

        Assert.Equal(request.EventType, command.Request.EventType);
        Assert.Equal(request.Email, command.Request.Email);
        Assert.Equal(request.Source, command.Request.Source);
        Assert.Equal(request.Classification, command.Request.Classification);
        Assert.Equal(request.ProviderMessageId, command.Request.ProviderMessageId);
        Assert.Equal(request.Reason, command.Request.Reason);
        Assert.Equal(occurredAt, command.Request.OccurredAtUtc);
    }

    [Fact]
    public void DomainDeliveryEventRequests_ToCommands_MapSingleAndMany() {
        var request = new IngestMailEventRequest("complaint", "user@example.com", "aws-ses-sns");
        IReadOnlyList<IngestMailEventRequest> requests = [request];

        var single = request.ToCommand();
        var many = requests.ToCommand();

        Assert.IsType<IngestMailRelayDeliveryEventCommand>(single);
        Assert.Same(request, single.Request);
        Assert.IsType<IngestManyMailRelayDeliveryEventsCommand>(many);
        Assert.Same(requests, many.Requests);
    }

    [Fact]
    public void AwsSesWebhook_ToMappedCommand_WhenValidBounce_MapsManyCommand() {
        var request = new AwsSesSnsWebhookHttpRequest(
            Type: "Notification",
            Message: """
                {
                  "notificationType": "Bounce",
                  "mail": { "messageId": "ses-message-1", "destination": ["a@example.com"] },
                  "bounce": {
                    "bounceType": "Permanent",
                    "bouncedRecipients": [
                      { "emailAddress": "a@example.com", "diagnosticCode": "smtp; 550" }
                    ]
                  }
                }
                """);

        var mapped = request.ToMappedCommand();

        Assert.True(mapped.IsSuccess);
        var command = Assert.IsType<IngestManyMailRelayDeliveryEventsCommand>(mapped.Request);
        var deliveryEvent = Assert.Single(command.Requests);
        Assert.Equal("bounce", deliveryEvent.EventType);
        Assert.Equal("a@example.com", deliveryEvent.Email);
        Assert.Equal("hard", deliveryEvent.Classification);
        Assert.Equal("ses-message-1", deliveryEvent.ProviderMessageId);
        Assert.Equal("smtp; 550", deliveryEvent.Reason);
    }

    [Fact]
    public void AwsSesWebhook_ToMappedCommand_WhenInvalid_ReturnsFailure() {
        var request = new AwsSesSnsWebhookHttpRequest(Type: "SubscriptionConfirmation", Message: "{}");

        var mapped = request.ToMappedCommand();

        Assert.False(mapped.IsSuccess);
        Assert.Null(mapped.Request);
        Assert.Contains("Type=Notification", mapped.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void MailgunWebhook_ToMappedCommand_WhenValidBounce_MapsSingleCommand() {
        var request = new MailgunWebhookHttpRequest(new MailgunEventDataHttpModel(
            Event: "failed",
            Recipient: "a@example.com",
            Id: "mailgun-1",
            Severity: "permanent",
            Reason: "smtp; 550"));

        var mapped = request.ToMappedCommand();

        Assert.True(mapped.IsSuccess);
        var command = Assert.IsType<IngestMailRelayDeliveryEventCommand>(mapped.Request);
        Assert.Equal("bounce", command.Request.EventType);
        Assert.Equal("a@example.com", command.Request.Email);
        Assert.Equal("hard", command.Request.Classification);
        Assert.Equal("mailgun-1", command.Request.ProviderMessageId);
    }

    [Fact]
    public void MailgunWebhook_ToMappedCommand_WhenUnsupported_ReturnsFailure() {
        var request = new MailgunWebhookHttpRequest(new MailgunEventDataHttpModel(
            Event: "opened",
            Recipient: "a@example.com"));

        var mapped = request.ToMappedCommand();

        Assert.False(mapped.IsSuccess);
        Assert.Null(mapped.Request);
        Assert.Contains("Unsupported Mailgun event", mapped.Error, StringComparison.Ordinal);
    }

    [Fact]
    public void GuidAndSuppressionHelpers_MapResponsesAndCommands() {
        var id = Guid.NewGuid();

        Assert.Equal(id, id.ToEnqueuedHttpResponse().Id);
        Assert.Equal("queued", id.ToEnqueuedHttpResponse().Status);
        Assert.Equal("suppressed", MailRelayEmailHttpMappings.ToSuppressionCreatedHttpResponse().Status);
        Assert.Equal("blocked@example.com", "blocked@example.com".ToRemoveSuppressionCommand().Email);
    }

    [Fact]
    public void QueueStats_ToHttpResponse_MapsAllCounts() {
        var stats = new MailRelayQueueStats(
            PendingCount: 1,
            RetryCount: 2,
            ProcessingCount: 3,
            SentCount: 4,
            FailedCount: 5,
            SuppressedCount: 6);

        var response = stats.ToHttpResponse();

        Assert.Equal(1, response.PendingCount);
        Assert.Equal(2, response.RetryCount);
        Assert.Equal(3, response.ProcessingCount);
        Assert.Equal(4, response.SentCount);
        Assert.Equal(5, response.FailedCount);
        Assert.Equal(6, response.SuppressedCount);
    }

    [Fact]
    public void MessageDetails_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddHours(-2);
        var availableAt = DateTimeOffset.UtcNow.AddHours(-1);
        var lockedAt = DateTimeOffset.UtcNow.AddMinutes(-30);
        var sentAt = DateTimeOffset.UtcNow;
        var message = new MailRelayMessageDetails(
            Id: id,
            Status: "sent",
            Subject: "Subject",
            CorrelationId: "corr-1",
            AttemptCount: 2,
            MaxAttempts: 5,
            CreatedAtUtc: createdAt,
            AvailableAtUtc: availableAt,
            LockedAtUtc: lockedAt,
            SentAtUtc: sentAt,
            LastError: "last error",
            SuppressedRecipients: ["blocked@example.com"]);

        var response = message.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("sent", response.Status);
        Assert.Equal("Subject", response.Subject);
        Assert.Equal("corr-1", response.CorrelationId);
        Assert.Equal(2, response.AttemptCount);
        Assert.Equal(5, response.MaxAttempts);
        Assert.Equal(createdAt, response.CreatedAtUtc);
        Assert.Equal(availableAt, response.AvailableAtUtc);
        Assert.Equal(lockedAt, response.LockedAtUtc);
        Assert.Equal(sentAt, response.SentAtUtc);
        Assert.Equal("last error", response.LastError);
        Assert.Equal(["blocked@example.com"], response.SuppressedRecipients);
    }

    [Fact]
    public void SuppressionsAndDeliveryEvents_ToHttpResponse_MapLists() {
        var now = DateTimeOffset.UtcNow;
        IReadOnlyList<MailRelaySuppressionEntry> suppressions = [
            new("blocked@example.com", "bounce", "aws", now.AddMinutes(-2), now.AddMinutes(-1), now.AddDays(7))
        ];
        IReadOnlyList<MailRelayDeliveryEventEntry> deliveryEvents = [
            new(Guid.NewGuid(), "bounce", "blocked@example.com", "aws", "hard", "provider-1", "smtp; 550", now, now)
        ];

        var suppressionResponse = Assert.Single(suppressions.ToHttpResponse());
        var eventResponse = Assert.Single(deliveryEvents.ToHttpResponse());
        var ingestionResponse = deliveryEvents.ToProviderIngestionHttpResponse();

        Assert.Equal("blocked@example.com", suppressionResponse.Email);
        Assert.Equal("bounce", suppressionResponse.Reason);
        Assert.Equal("aws", suppressionResponse.Source);
        Assert.Equal("bounce", eventResponse.EventType);
        Assert.Equal("hard", eventResponse.Classification);
        Assert.Equal(1, ingestionResponse.Accepted);
    }
}
