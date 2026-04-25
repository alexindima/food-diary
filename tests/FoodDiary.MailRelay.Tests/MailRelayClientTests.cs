using System.Net;
using System.Net.Http.Json;
using FoodDiary.MailRelay.Client;
using FoodDiary.MailRelay.Client.Models;
using FoodDiary.MailRelay.Client.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailRelay.Tests;

public sealed class MailRelayClientTests {
    [Fact]
    public async Task EnqueueAsync_SendsExpectedRequestAndApiKeyHeader() {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Created) {
            Content = JsonContent.Create(new EnqueueMailRelayEmailResponse(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "queued"))
        });
        using var httpClient = new HttpClient(handler) {
            BaseAddress = new Uri("https://relay.example.test")
        };
        var client = new MailRelayClient(httpClient, Options.Create(new MailRelayClientOptions {
            BaseUrl = "https://relay.example.test",
            ApiKey = "secret"
        }));

        var response = await client.EnqueueAsync(CreateRequest(), CancellationToken.None);

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), response.Id);
        Assert.Equal(HttpMethod.Post, handler.Request?.Method);
        Assert.Equal("https://relay.example.test/api/email/send", handler.Request?.RequestUri?.ToString());
        Assert.Equal("secret", handler.Request?.Headers.GetValues("X-Relay-Api-Key").Single());
        var payload = System.Text.Json.JsonSerializer.Deserialize<EnqueueMailRelayEmailRequest>(
            handler.RequestBody!,
            new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
        Assert.Equal("user@example.com", payload!.To.Single());
    }

    [Fact]
    public async Task EnqueueAsync_WhenBaseAddressIsMissing_Throws() {
        using var httpClient = new HttpClient(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new MailRelayClient(httpClient, Options.Create(new MailRelayClientOptions()));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.EnqueueAsync(CreateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task EnqueueAsync_WhenResponseIsEmpty_Throws() {
        using var httpClient = new HttpClient(new RecordingHandler(new HttpResponseMessage(HttpStatusCode.Accepted))) {
            BaseAddress = new Uri("https://relay.example.test")
        };
        var client = new MailRelayClient(httpClient, Options.Create(new MailRelayClientOptions {
            BaseUrl = "https://relay.example.test"
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => client.EnqueueAsync(CreateRequest(), CancellationToken.None));
    }

    private static EnqueueMailRelayEmailRequest CreateRequest() =>
        new(
            "relay@example.com",
            "FoodDiary",
            ["user@example.com"],
            "Subject",
            "<p>Hello</p>",
            "Hello",
            "correlation",
            "key");

    private sealed class RecordingHandler(HttpResponseMessage response) : HttpMessageHandler {
        public HttpRequestMessage? Request { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Request = request;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return response;
        }
    }
}
