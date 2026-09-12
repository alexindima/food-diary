using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class BotDiaryClient(HttpClient client, IOptions<TelegramBotOptions> options) {
    internal const string ClientName = "TelegramDiary";
    private const long MaximumResponseBytes = 262144;

    internal async Task<BotDiaryStatistics> GetStatisticsAsync(string token, int days, CancellationToken cancellationToken) {
        if (days is not (1 or 7)) {
            throw new InvalidDataException("Invalid statistics period.");
        }
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get,
            $"/api/v1/statistics/diary-summary?days={days.ToString(CultureInfo.InvariantCulture)}", token);
        BotDiaryStatistics result = await SendAsync<BotDiaryStatistics>(request, cancellationToken).ConfigureAwait(false);
        if (result.CalendarDays != days || result.Days is null || result.Days.Count != days) {
            throw new InvalidDataException("Statistics response does not match the requested period.");
        }
        return result;
    }

    internal async Task<string> AuthenticateAsync(long telegramUserId, BotOperationLease lease, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, "/api/v1/auth/telegram/bot/auth", accessToken: null);
        request.Headers.Add("X-Telegram-Bot-Secret", options.Value.ApiSecret);
        request.Content = JsonContent.Create(new { TelegramUserId = telegramUserId });
        AuthReply reply = await SendAsync<AuthReply>(request, cancellationToken).ConfigureAwait(false);
        if (reply.User.Id != lease.UserId || !MatchesSecurityVersion(reply.AccessToken, lease.SecurityVersion)) {
            throw new InvalidDataException("Telegram authentication no longer matches the operation owner.");
        }
        return reply.AccessToken;
    }

    internal async Task<BotImageUpload> RequestUploadAsync(string token, Guid operationId, int attempt, string contentType,
        int sizeBytes, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, "/api/v1/images/upload-url", token);
        request.Headers.Add("Idempotency-Key", $"telegram:{operationId:N}:upload:{attempt.ToString(CultureInfo.InvariantCulture)}");
        string extension = contentType switch { "image/png" => "png", "image/webp" => "webp", _ => "jpg" };
        request.Content = JsonContent.Create(new { FileName = $"{operationId:N}.{extension}", ContentType = contentType, FileSizeBytes = sizeBytes });
        return await SendAsync<BotImageUpload>(request, cancellationToken).ConfigureAwait(false);
    }

    internal async Task UploadAsync(BotImageUpload upload, string contentType, byte[] content, CancellationToken cancellationToken) {
        if (!Uri.TryCreate(upload.UploadUrl, UriKind.Absolute, out Uri? uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal) || uri.UserInfo.Length != 0 || uri.Fragment.Length != 0) {
            throw new InvalidDataException("The API returned an invalid image upload URL.");
        }
        using var request = new HttpRequestMessage(HttpMethod.Put, uri) { Content = new ByteArrayContent(content) };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    internal async Task ConfirmUploadAsync(string token, Guid assetId, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"/api/v1/images/{assetId:D}/confirm", token);
        request.Headers.Add("Idempotency-Key", $"telegram:{assetId:N}:confirm");
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    internal async Task<BotRecognitionJob> StartRecognitionAsync(string token, Guid operationId, Guid assetId,
        string? caption, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, "/api/v1/ai/food/recognitions", token);
        request.Content = JsonContent.Create(new { Id = operationId, ImageAssetId = assetId, Description = caption });
        return await SendAsync<BotRecognitionJob>(request, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<BotRecognitionJob> GetRecognitionAsync(string token, Guid jobId, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Get, $"/api/v1/ai/food/recognitions/{jobId:D}", token);
        return await SendAsync<BotRecognitionJob>(request, cancellationToken).ConfigureAwait(false);
    }

    internal async Task<BotRecognizedMeal> SaveRecognizedMealAsync(string token, Guid recognitionId, DateTime occurredAtUtc, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"/api/v1/meals/recognitions/{recognitionId:D}", token);
        request.Content = JsonContent.Create(new { OccurredAtUtc = occurredAtUtc });
        BotRecognizedMeal result = await SendAsync<BotRecognizedMeal>(request, cancellationToken).ConfigureAwait(false);
        if (result.OperationId != recognitionId || result.MealId == Guid.Empty) {
            throw new InvalidDataException("Meal receipt identity mismatch.");
        }
        return result;
    }

    internal async Task<string> UndoRecognizedMealAsync(string token, Guid operationId, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"/api/v1/meals/recognitions/{operationId:D}/undo", token);
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.NotFound) {
            ApiErrorReply? error = await response.Content.ReadFromJsonAsync<ApiErrorReply>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return error?.Error ?? "Meal.RecognitionOperationNotFound";
        }
        response.EnsureSuccessStatusCode();
        UndoReply? reply = await response.Content.ReadFromJsonAsync<UndoReply>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return reply?.Status ?? throw new InvalidDataException("Missing undo result.");
    }

    private sealed record UndoReply(string Status);
    internal async Task<BotHydrationReceipt> SaveWaterAsync(string token, Guid operationId, DateTime timestampUtc, int amountMl, CancellationToken cancellationToken) {
        using HttpRequestMessage request = CreateRequest(HttpMethod.Post, $"/api/v1/hydrations/operations/{operationId:D}", token);
        request.Content = JsonContent.Create(new { TimestampUtc = timestampUtc, AmountMl = amountMl });
        BotHydrationReceipt receipt = await SendAsync<BotHydrationReceipt>(request, cancellationToken).ConfigureAwait(false);
        if (receipt.OperationId != operationId || receipt.EntryId == Guid.Empty || receipt.AmountMl != amountMl ||
            receipt.TimestampUtc.Ticks / 10 != timestampUtc.Ticks / 10) {
            throw new InvalidDataException("Water receipt does not match the requested operation.");
        }
        return receipt;
    }

    private sealed record ApiErrorReply(string Error);

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, string? accessToken) {
        if (!BotUriHelper.TryCreateApiBaseUri(options.Value.ApiBaseUrl, out Uri? baseUri)) {
            throw new InvalidOperationException("Telegram diary API is not configured.");
        }
        var request = new HttpRequestMessage(method, new Uri(baseUri!, path));
        if (accessToken is not null) {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests) {
            await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
            try {
                ApiErrorReply? error = await response.Content.ReadFromJsonAsync<ApiErrorReply>(cancellationToken: cancellationToken).ConfigureAwait(false);
                if (error?.Error is "Ai.ConsentRequired" or "Ai.QuotaExceeded") {
                    throw new BotRecognitionAccessException(error.Error);
                }
            } catch (JsonException) {
                // Non-JSON gateway responses retain normal HTTP retry handling.
            }
        }
        response.EnsureSuccessStatusCode();
        await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException("The diary API returned an empty response.");
    }

    private static bool MatchesSecurityVersion(string token, long expected) {
        string[] parts = token.Split('.');
        if (parts.Length != 3 || token.Length > 32768) {
            return false;
        }
        try {
            string base64 = parts[1].Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight((base64.Length + 3) / 4 * 4, '=');
            using var payload = JsonDocument.Parse(Convert.FromBase64String(base64));
            return payload.RootElement.TryGetProperty("security_version", out JsonElement version) &&
                version.ValueKind == JsonValueKind.String &&
                long.TryParse(version.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out long actual) && actual == expected;
        } catch (FormatException) {
            return false;
        } catch (JsonException) {
            return false;
        }
    }

    private sealed record AuthReply(string AccessToken, AuthUser User);
    private sealed record AuthUser(Guid Id);
}
