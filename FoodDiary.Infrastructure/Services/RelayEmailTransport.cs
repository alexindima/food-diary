using System.Net.Mail;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using FoodDiary.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Infrastructure.Services;

internal sealed class RelayEmailTransport(
    IHttpClientFactory httpClientFactory,
    IOptions<EmailDeliveryOptions> deliveryOptions) : IEmailTransport {
    public const string HttpClientName = "FoodDiary.MailRelay";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly EmailDeliveryOptions _deliveryOptions = deliveryOptions.Value;

    public async Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(_deliveryOptions.RelayBaseUrl)) {
            throw new InvalidOperationException("Email relay base URL is not configured.");
        }

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.BaseAddress = new Uri(_deliveryOptions.RelayBaseUrl, UriKind.Absolute);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/email/send") {
            Content = JsonContent.Create(CreatePayload(message), options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(_deliveryOptions.RelayApiKey)) {
            request.Headers.TryAddWithoutValidation("X-Relay-Api-Key", _deliveryOptions.RelayApiKey);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static RelayEmailMessageRequest CreatePayload(MailMessage message) {
        if (message.From is null) {
            throw new InvalidOperationException("Email message must include a From address.");
        }

        return new RelayEmailMessageRequest(
            message.From.Address,
            message.From.DisplayName,
            message.To.Select(static recipient => recipient.Address).ToArray(),
            message.Subject,
            message.Body,
            GetPlainTextBody(message.AlternateViews),
            CorrelationId: Guid.NewGuid().ToString("N"));
    }

    private static string? GetPlainTextBody(AlternateViewCollection alternateViews) {
        foreach (var view in alternateViews) {
            if (!string.Equals(view.ContentType.MediaType, MediaTypeNames.Text.Plain, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (view.ContentStream.CanSeek) {
                view.ContentStream.Position = 0;
            }

            using var reader = new StreamReader(view.ContentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = reader.ReadToEnd();
            if (view.ContentStream.CanSeek) {
                view.ContentStream.Position = 0;
            }

            return content;
        }

        return null;
    }

    private sealed record RelayEmailMessageRequest(
        string FromAddress,
        string FromName,
        IReadOnlyList<string> To,
        string Subject,
        string HtmlBody,
        string? TextBody,
        string? CorrelationId = null,
        string? IdempotencyKey = null);
}
