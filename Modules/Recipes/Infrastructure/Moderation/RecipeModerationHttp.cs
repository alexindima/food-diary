using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Integrations.Http.Http;
using FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal static class RecipeModerationHttp {
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(20);
    private static readonly JsonSerializerOptions ResponseJsonOptions = new() {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };

    public static async Task<RecipeModerationCheck> SendAsync<TInput>(
        HttpClient httpClient,
        Uri endpoint,
        string apiKey,
        string model,
        TInput input,
        string field,
        IReadOnlyList<string>? requiredCategories,
        CancellationToken cancellationToken) {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(RequestTimeout);
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = JsonContent.Create(new { model, input });

        try {
            using HttpResponseMessage response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode) {
                HttpStatusCode status = response.StatusCode;
                bool retryable = status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;
                throw new RecipeModerationProviderException(
                    string.Create(CultureInfo.InvariantCulture, $"Moderation provider returned HTTP {(int)status}."),
                    retryable,
                    retryable ? GetRetryDelay(response.Headers.RetryAfter) : null);
            }

            ModerationResponse? payload = await BoundedHttpContentReader.ReadFromJsonAsync<ModerationResponse>(
                response.Content,
                ResponseJsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                deadline.Token);

            return Decode(payload, field, requiredCategories);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            throw new RecipeModerationProviderException("Moderation provider timed out.", retryable: true);
        } catch (TimeoutException) {
            throw new RecipeModerationProviderException("Moderation provider timed out.", retryable: true);
        } catch (Exception exception) when (exception is JsonException or InvalidDataException) {
            throw InvalidResponse();
        } catch (Exception exception) when (exception is HttpRequestException or IOException) {
            throw new RecipeModerationProviderException("Moderation provider connection failed.", retryable: true);
        }
    }

    private static RecipeModerationCheck Decode(
        ModerationResponse? payload, string field, IReadOnlyList<string>? requiredCategories) {
        if (payload is not { Results: [{ Categories.Count: > 0, CategoryScores.Count: > 0 } result] }
            || string.IsNullOrWhiteSpace(payload.Id)
            || string.IsNullOrWhiteSpace(payload.Model)) {
            throw InvalidResponse();
        }

        if (requiredCategories is not null && (result.Flagged is null
            || requiredCategories.Any(category => !result.Categories.TryGetValue(category, out bool? flag) || flag is null))) {
            throw InvalidResponse();
        }

        var categories = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, bool?> category in result.Categories) {
            if (category.Value is bool flag) {
                if (!result.CategoryScores.ContainsKey(category.Key)) {
                    throw InvalidResponse();
                }

                categories.Add(category.Key, flag);
            }
        }

        if (categories.Count == 0 || result.CategoryScores.Values.Any(static score => score is < 0 or > 1 || !double.IsFinite(score))) {
            throw InvalidResponse();
        }

        bool flagged = categories.ContainsValue(true);
        if (requiredCategories is not null && result.Flagged != flagged) {
            throw InvalidResponse();
        }

        var signal = new RecipeModerationSignal(field, payload.Id, categories, result.CategoryScores, flagged);
        return new RecipeModerationCheck(payload.Model, [signal]);
    }

    private static TimeSpan? GetRetryDelay(RetryConditionHeaderValue? retryAfter) {
        TimeSpan? delay = retryAfter?.Delta ?? retryAfter?.Date - DateTimeOffset.UtcNow;
        return delay > TimeSpan.Zero ? delay : null;
    }

    private static RecipeModerationProviderException InvalidResponse() =>
        new("Moderation provider returned an incomplete or invalid result.", retryable: true);

    private sealed record ModerationResponse(string? Id, string? Model, ModerationResult?[]? Results);

    private sealed record ModerationResult(
        bool? Flagged, Dictionary<string, bool?>? Categories, Dictionary<string, double>? CategoryScores);
}
