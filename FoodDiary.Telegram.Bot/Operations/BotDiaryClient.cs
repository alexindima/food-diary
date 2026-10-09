using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using FoodDiary.Telegram.Bot.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Api;
using FoodDiary.Telegram.Bot.Api.Generated.Model;

namespace FoodDiary.Telegram.Bot.Operations;

internal sealed class BotDiaryClient(HttpClient client, IOptions<TelegramBotOptions> options) {
    internal const string ClientName = "TelegramDiary";
    private const long MaximumResponseBytes = 262144;

    internal async Task<BotDiaryStatistics> GetStatisticsAsync(string token, int days, CancellationToken cancellationToken) {
        if (days is not (1 or 7)) {
            throw new InvalidDataException("Invalid statistics period.");
        }
        using HttpResponseMessage response = await new BotDiaryApi(CreateTransport()).GetStatisticsAsync("1", days,
            new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        DiaryStatisticsSummaryHttpResponse result = await ReadAsync<DiaryStatisticsSummaryHttpResponse>(response, cancellationToken).ConfigureAwait(false);
        if (result.CalendarDays != days || result.Days is null || result.Days.Count != days) {
            throw new InvalidDataException("Statistics response does not match the requested period.");
        }
        return BotApiMapper.Statistics(result);
    }

    internal async Task<string> AuthenticateAsync(long telegramUserId, BotOperationLease lease, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotAuthApi(CreateTransport()).AuthenticateAsync("1",
            new TelegramBotAuthHttpRequest { TelegramUserId = telegramUserId }, new BotApiRequestContext(ApiSecret: options.Value.ApiSecret),
            cancellationToken).ConfigureAwait(false);
        AuthenticationHttpResponse reply = await ReadAsync<AuthenticationHttpResponse>(response, cancellationToken).ConfigureAwait(false);
        if (reply.User is null || new BotUserId(reply.User.Id) != lease.UserId || string.IsNullOrWhiteSpace(reply.AccessToken) ||
            !MatchesSecurityVersion(reply.AccessToken, lease.SecurityVersion)) {
            throw new InvalidDataException("Telegram authentication no longer matches the operation owner.");
        }
        return reply.AccessToken;
    }

    internal async Task<BotImageUpload> RequestUploadAsync(string token, BotOperationId operationId, int attempt, string contentType,
        int sizeBytes, CancellationToken cancellationToken) {
        string extension = contentType switch { "image/png" => "png", "image/webp" => "webp", _ => "jpg" };
        using HttpResponseMessage response = await new BotImagesApi(CreateTransport()).RequestUploadAsync("1",
            $"telegram:{operationId.Value:N}:upload:{attempt.ToString(CultureInfo.InvariantCulture)}",
            new GetImageUploadUrlHttpRequest { FileName = $"{operationId.Value:N}.{extension}", ContentType = contentType, FileSizeBytes = sizeBytes },
            new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        return BotApiMapper.Upload(await ReadAsync<GetImageUploadUrlHttpResponse>(response, cancellationToken).ConfigureAwait(false));
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

    internal async Task ConfirmUploadAsync(string token, BotImageAssetId assetId, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotImagesApi(CreateTransport()).ConfirmUploadAsync(assetId.Value, "1",
            $"telegram:{assetId.Value:N}:confirm", new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    internal async Task<BotRecognitionJob> StartRecognitionAsync(string token, BotOperationId operationId, BotImageAssetId assetId,
        string? caption, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotRecognitionApi(CreateTransport()).StartRecognitionAsync("1",
            new StartFoodRecognitionHttpRequest { Id = operationId.Value, ImageAssetId = assetId.Value, Description = caption },
            new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        return BotApiMapper.Recognition(await ReadAsync<FoodRecognitionJobHttpResponse>(response, cancellationToken).ConfigureAwait(false));
    }

    internal async Task<BotRecognitionJob> GetRecognitionAsync(string token, BotRecognitionId jobId, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotRecognitionApi(CreateTransport()).GetRecognitionAsync(jobId.Value, "1",
            new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        return BotApiMapper.Recognition(await ReadAsync<FoodRecognitionJobHttpResponse>(response, cancellationToken).ConfigureAwait(false));
    }

    internal async Task<BotRecognizedMeal> SaveRecognizedMealAsync(string token, BotRecognitionId recognitionId, DateTime occurredAtUtc, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotMealsApi(CreateTransport()).SaveRecognizedMealAsync(recognitionId.Value, "1",
            new CreateMealFromRecognitionHttpRequest { OccurredAtUtc = occurredAtUtc }, new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        BotRecognizedMeal result = BotApiMapper.Meal(await ReadAsync<RecognizedMealCreationHttpResponse>(response, cancellationToken).ConfigureAwait(false));
        if (result.OperationId.Value != recognitionId.Value || result.MealId.Value == Guid.Empty) {
            throw new InvalidDataException("Meal receipt identity mismatch.");
        }
        return result;
    }

    internal async Task<string> UndoRecognizedMealAsync(string token, BotOperationId operationId, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotMealsApi(CreateTransport()).UndoRecognizedMealAsync(operationId.Value, "1",
            new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode is System.Net.HttpStatusCode.Conflict or System.Net.HttpStatusCode.NotFound) {
            ApiErrorHttpResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorHttpResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
            return error?.Error ?? "Meal.RecognitionOperationNotFound";
        }
        response.EnsureSuccessStatusCode();
        RecognizedMealUndoHttpResponse? reply = await response.Content.ReadFromJsonAsync<RecognizedMealUndoHttpResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
        return reply?.Status ?? throw new InvalidDataException("Missing undo result.");
    }

    internal async Task<BotHydrationReceipt> SaveWaterAsync(string token, BotOperationId operationId, DateTime timestampUtc, int amountMl, CancellationToken cancellationToken) {
        using HttpResponseMessage response = await new BotHydrationApi(CreateTransport()).SaveWaterAsync(operationId.Value, "1",
            new CreateHydrationFromOperationHttpRequest { TimestampUtc = timestampUtc, AmountMl = amountMl },
            new BotApiRequestContext(token), cancellationToken).ConfigureAwait(false);
        BotHydrationReceipt receipt = BotApiMapper.Water(await ReadAsync<HydrationOperationHttpResponse>(response, cancellationToken).ConfigureAwait(false));
        if (receipt.OperationId != operationId || receipt.EntryId.Value == Guid.Empty || receipt.AmountMl != amountMl ||
            receipt.TimestampUtc.Ticks / 10 != timestampUtc.Ticks / 10) {
            throw new InvalidDataException("Water receipt does not match the requested operation.");
        }
        return receipt;
    }

    private BotApiTransport CreateTransport() {
        if (!BotUriHelper.TryCreateApiBaseUri(options.Value.ApiBaseUrl, out Uri? baseUri)) {
            throw new InvalidOperationException("Telegram diary API is not configured.");
        }
        return new BotApiTransport(client, baseUri!);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) {
        if (response.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests) {
            await response.Content.LoadIntoBufferAsync(MaximumResponseBytes, cancellationToken).ConfigureAwait(false);
            try {
                ApiErrorHttpResponse? error = await response.Content.ReadFromJsonAsync<ApiErrorHttpResponse>(cancellationToken: cancellationToken).ConfigureAwait(false);
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
            return payload.RootElement.ValueKind == JsonValueKind.Object &&
                payload.RootElement.TryGetProperty("security_version", out JsonElement version) &&
                version.ValueKind == JsonValueKind.String &&
                long.TryParse(version.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out long actual) && actual == expected;
        } catch (FormatException) {
            return false;
        } catch (JsonException) {
            return false;
        }
    }

}
