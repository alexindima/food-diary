using System.Diagnostics.Metrics;
using System.Net.Http.Json;
using FoodDiary.MailRelay.Presentation.Features.Email.Mappings;
using FoodDiary.MailRelay.Presentation.Features.Email.Requests;
using FoodDiary.MailRelay.Application.DeliveryEvents.Models;
using FoodDiary.MailRelay.Application.Emails.Models;
using FoodDiary.MailRelay.Application.Telemetry;
using FoodDiary.MailRelay.Tests.TestInfrastructure;

namespace FoodDiary.MailRelay.Tests;

[Collection("mailrelay-environment")]
public sealed class MailRelayIntegrationTests(MailRelayEnvironmentFixture fixture) {
    [RequiresDockerFact]
    public async Task SendEndpoint_WhenRelayPipelineSucceeds_ProcessesQueuedMessageAndRecordsTelemetry() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport();
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using var client = factory.CreateClient();

        long? deliveryCount = null;
        string? deliveryOutcome = null;
        using var listener = CreateMailRelayListener((instrument, value, tags) => {
            if (instrument.Name != "fooddiary.mailrelay.delivery.events") {
                return;
            }

            deliveryCount = value;
            deliveryOutcome = GetTagValue(tags, "fooddiary.mailrelay.delivery.outcome");
        });

        var response = await client.PostAsJsonAsync("/api/email/send", new RelayEmailMessageRequest(
            "noreply@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Verify email",
            "<p>Hello</p>",
            "Hello"));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        Assert.NotNull(payload);

        await WaitForAsync(async () => {
            var message = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}");
            return message?.Status == "sent";
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
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/email/send", new RelayEmailMessageRequest(
            "noreply@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Verify email",
            "<p>Hello</p>",
            "Hello"));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        Assert.NotNull(payload);

        await WaitForAsync(async () => {
            var message = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}");
            return message?.Status == "failed";
        }, timeout: TimeSpan.FromSeconds(20));

        var failedMessage = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}");
        Assert.Equal("failed", failedMessage?.Status);
        Assert.NotNull(failedMessage?.LastError);
    }

    [RequiresDockerFact]
    public async Task SendEndpoint_WhenRecipientIsSuppressed_DoesNotDeliverAndMarksMessageSuppressed() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport();
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using var client = factory.CreateClient();

        var suppressionResponse = await client.PostAsJsonAsync("/api/email/suppressions", new CreateSuppressionRequest(
            "user@example.com",
            "hard-bounce",
            "integration-test"));
        suppressionResponse.EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/email/send", new RelayEmailMessageRequest(
            "noreply@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Verify email",
            "<p>Hello</p>",
            "Hello"));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<QueuedResponse>();
        Assert.NotNull(payload);

        await WaitForAsync(async () => {
            var message = await client.GetFromJsonAsync<MessageDetails>($"/api/email/messages/{payload!.Id}");
            return message?.Status == "suppressed";
        });

        Assert.Empty(transport.SentMessages);
    }

    [RequiresDockerFact]
    public async Task DeliveryEvent_WhenHardBounceIsReported_AddsSuppressionAndStoresEvent() {
        fixture.EnsureAvailable();
        var transport = new RecordingRelayDeliveryTransport();
        await using var factory = new MailRelayWebApplicationFactory(fixture, transport);
        using var client = factory.CreateClient();

        var eventResponse = await client.PostAsJsonAsync("/api/email/events", new IngestMailEventRequest(
            "bounce",
            "user@example.com",
            "integration-test",
            "hard",
            "provider-1",
            "mailbox-does-not-exist"));
        eventResponse.EnsureSuccessStatusCode();

        var suppressions = await client.GetFromJsonAsync<List<SuppressionEntry>>("/api/email/suppressions?email=user@example.com");
        Assert.NotNull(suppressions);
        Assert.Single(suppressions);
        Assert.Equal("user@example.com", suppressions[0].Email);

        var events = await client.GetFromJsonAsync<List<DeliveryEventEntry>>("/api/email/events?email=user@example.com");
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

        var mapped = payload.TryMapToDeliveryEvents(out var events, out var error);

        Assert.True(mapped);
        Assert.Null(error);
        Assert.Single(events);
        Assert.Equal("bounce", events[0].EventType);
        Assert.Equal("hard", events[0].Classification);
        Assert.Equal("user@example.com", events[0].Email);
    }

    [Fact]
    public void MailgunEventMapper_WhenComplaintEvent_ReturnsComplaint() {
        var payload = new MailgunWebhookHttpRequest(new MailgunEventDataHttpModel(
            "complained",
            "user@example.com",
            "mailgun-1",
            null,
            "spam-complaint"));

        var mapped = payload.TryMapToDeliveryEvent(out var deliveryEvent, out var error);

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
            if (instrument.Meter.Name == MailRelayTelemetry.MeterName) {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => onMeasurement(instrument, value, tags));
        listener.Start();
        return listener;
    }

    private static string? GetTagValue(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key) {
        foreach (var tag in tags) {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal)) {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition, TimeSpan? timeout = null) {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(10));
        while (DateTime.UtcNow < deadline) {
            if (await condition()) {
                return;
            }

            await Task.Delay(250);
        }

        throw new TimeoutException("Condition was not satisfied in time.");
    }

    private sealed record QueuedResponse(Guid Id, string Status);

    private sealed record MessageDetails(Guid Id, string Status, string? LastError);

    private sealed record SuppressionEntry(string Email, string Reason, string Source);

    private sealed record DeliveryEventEntry(string EventType, string Email, string? Classification);
}
