using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Application.Telemetry;
using FoodDiary.MailRelay.Domain.DeliveryEvents;
using FoodDiary.MailRelay.Domain.Emails;
using FoodDiary.MailRelay.Tests.TestInfrastructure;

namespace FoodDiary.MailRelay.Tests;

[Collection("mailrelay-environment")]
[ExcludeFromCodeCoverage]
public sealed class MailRelayIntegrationTests(MailRelayEnvironmentFixture fixture) {
    [RequiresDockerFact]
    public async Task SendEndpoint_WhenRelayPipelineSucceeds_ProcessesQueuedMessageAndRecordsTelemetry() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport();
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using HttpClient client = factory.CreateClient();
        AddRelayApiKey(client);

        long? deliveryCount = null;
        string? deliveryOutcome = null;
        using MeterListener listener = CreateMailRelayListener((instrument, value, tags) => {
            if (!string.Equals(instrument.Name, "fooddiary.mailrelay.delivery.events", StringComparison.Ordinal)) {
                return;
            }

            deliveryCount = value;
            deliveryOutcome = GetTagValue(tags, "fooddiary.mailrelay.delivery.outcome");
        });

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/email/send", new EnqueueMailRelayEmailRequest(
            "noreply@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Verify email",
            "<p>Hello</p>",
            "Hello"));

        response.EnsureSuccessStatusCode();
        QueuedResponse? payload = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        Assert.NotNull(payload);

        await WaitForAsync(async () => {
            MessageDetails? message = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}").ConfigureAwait(false);
            return string.Equals(message?.Status, "sent", StringComparison.Ordinal);
        });

        Assert.Single(transport.SentMessages);
        Assert.Equal(1, deliveryCount);
        Assert.Equal("success", deliveryOutcome);
    }

    [RequiresDockerFact]
    public async Task SendEndpoint_WhenDeliveryKeepsFailing_MarksMessageFailed() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport(remainingFailures: 5);
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using HttpClient client = factory.CreateClient();
        AddRelayApiKey(client);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/email/send", new EnqueueMailRelayEmailRequest(
            "noreply@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Verify email",
            "<p>Hello</p>",
            "Hello"));

        response.EnsureSuccessStatusCode();
        QueuedResponse? payload = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        Assert.NotNull(payload);

        await WaitForAsync(async () => {
            MessageDetails? message = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}").ConfigureAwait(false);
            return string.Equals(message?.Status, "failed", StringComparison.Ordinal);
        }, timeout: TimeSpan.FromSeconds(20));

        MessageDetails? failedMessage = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}");
        Assert.Equal("failed", failedMessage?.Status);
        Assert.NotNull(failedMessage?.LastError);
    }

    [RequiresDockerFact]
    public async Task SendEndpoint_WhenRecipientIsSuppressed_DoesNotDeliverAndMarksMessageSuppressed() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport();
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using HttpClient client = factory.CreateClient();
        AddRelayApiKey(client);

        HttpResponseMessage suppressionResponse = await client.PostAsJsonAsync("/api/email/suppressions", new CreateSuppressionRequest(
            "user@example.com",
            "hard-bounce",
            "integration-test"));
        suppressionResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/email/send", new EnqueueMailRelayEmailRequest(
            "noreply@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Verify email",
            "<p>Hello</p>",
            "Hello"));

        response.EnsureSuccessStatusCode();
        QueuedResponse? payload = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        Assert.NotNull(payload);

        await WaitForAsync(async () => {
            MessageDetails? message = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}").ConfigureAwait(false);
            return string.Equals(message?.Status, "suppressed", StringComparison.Ordinal);
        });

        Assert.Empty(transport.SentMessages);
    }

    [RequiresDockerFact]
    public async Task DeliveryEvent_WhenHardBounceIsReported_AddsSuppressionAndStoresEvent() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport();
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using HttpClient client = factory.CreateClient();
        AddRelayApiKey(client);

        HttpResponseMessage eventResponse = await client.PostAsJsonAsync("/api/email/events", new IngestMailEventRequest(
            "bounce",
            "user@example.com",
            "integration-test",
            "hard",
            "provider-1",
            "mailbox-does-not-exist"));
        eventResponse.EnsureSuccessStatusCode();

        List<SuppressionEntry>? suppressions = await client.GetFromJsonAsync<List<SuppressionEntry>>("/api/email/suppressions?email=user@example.com");
        Assert.NotNull(suppressions);
        Assert.Single(suppressions);
        Assert.Equal("user@example.com", suppressions[0].Email);

        List<DeliveryEventEntry>? events = await client.GetFromJsonAsync<List<DeliveryEventEntry>>("/api/email/events?email=user@example.com");
        Assert.NotNull(events);
        Assert.Single(events);
        Assert.Equal("bounce", events[0].EventType);
        Assert.Equal("hard", events[0].Classification);
    }

    [Fact]
    public void AwsSesSnsEventMapper_WhenPermanentBounce_ReturnsHardBounceEvents() {
        var payload = new AwsSesSnsWebhookHttpRequest(
            "Notification",
            """
            {
              "notificationType": "Bounce",
              "mail": {
                "messageId": "ses-message-1",
                "destination": ["user@example.com"]
              },
              "bounce": {
                "bounceType": "Permanent",
                "bouncedRecipients": [
                  { "emailAddress": "user@example.com", "diagnosticCode": "smtp; 550 user unknown" }
                ]
              }
            }
            """);

        bool mapped = payload.TryMapToDeliveryEvents(out IReadOnlyList<IngestMailEventRequest>? events, out string? error);

        Assert.True(mapped);
        Assert.Null(error);
        Assert.Single(events);
        Assert.Equal("bounce", events[0].EventType);
        Assert.Equal("hard", events[0].Classification);
        Assert.Equal("user@example.com", events[0].Email);
    }

    [Fact]
    public void MailgunEventMapper_WhenComplaintEvent_ReturnsComplaint() {
        var payload = new MailgunWebhookHttpRequest(new MailgunEventDataHttpRequest(
            "complained",
            "user@example.com",
            "mailgun-1",
            Severity: null,
            "spam-complaint"));

        bool mapped = payload.TryMapToDeliveryEvent(out IngestMailEventRequest? deliveryEvent, out string? error);

        Assert.True(mapped);
        Assert.Null(error);
        Assert.NotNull(deliveryEvent);
        Assert.Equal("complaint", deliveryEvent!.EventType);
        Assert.Equal("user@example.com", deliveryEvent.Email);
        Assert.Equal("mailgun-webhook", deliveryEvent.Source);
    }

    private static MeterListener CreateMailRelayListener(
        Action<Instrument, long, ReadOnlySpan<KeyValuePair<string, object?>>> onMeasurement) {
        var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) => {
            if (string.Equals(instrument.Meter.Name, MailRelayTelemetry.MeterName, StringComparison.Ordinal)) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => onMeasurement(instrument, value, tags));
        listener.Start();
        return listener;
    }

    private static void AddRelayApiKey(HttpClient client) =>
        client.DefaultRequestHeaders.Add("X-Relay-Api-Key", "integration-relay-api-key");

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (KeyValuePair<string, object?> tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition, TimeSpan? timeout = null) {
        DateTime deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline) {
            if (await condition().ConfigureAwait(false)) {
                return;
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        throw new TimeoutException("Condition was not satisfied in time.");
    }

    [ExcludeFromCodeCoverage]
    private sealed record QueuedResponse(Guid Id, string Status);

    [ExcludeFromCodeCoverage]
    private sealed record MessageDetails(Guid Id, string Status, string? LastError);

    [ExcludeFromCodeCoverage]
    private sealed record SuppressionEntry(string Email, string Reason, string Source);

    [ExcludeFromCodeCoverage]
    private sealed record DeliveryEventEntry(string EventType, string Email, string? Classification);
}
