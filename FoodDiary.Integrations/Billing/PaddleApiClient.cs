using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Http;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;

namespace FoodDiary.Integrations.Billing;

internal sealed class PaddleApiClient(HttpClient httpClient, PaddleOptions options) {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };

    public async Task<Result<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
        where TResponse : class {
        Result<PaddleEnvelope<TResponse>> envelopeResult = await SendEnvelopeAsync<TResponse>(
            method,
            path,
            body,
            cancellationToken).ConfigureAwait(false);
        if (envelopeResult.IsFailure) {
            return Result.Failure<TResponse>(envelopeResult.Error);
        }

        return envelopeResult.Value.Data is null
            ? Result.Failure<TResponse>(CreateProviderFailure("Paddle returned an empty response."))
            : Result.Success(envelopeResult.Value.Data);
    }

    public async Task<Result<PaddlePage<TItem>>> GetPageAsync<TItem>(
        string path,
        CancellationToken cancellationToken)
        where TItem : class {
        Result<PaddleEnvelope<List<TItem>>> envelopeResult = await SendEnvelopeAsync<List<TItem>>(
            HttpMethod.Get,
            NormalizeRelativePath(path),
            body: null,
            cancellationToken).ConfigureAwait(false);
        if (envelopeResult.IsFailure) {
            return Result.Failure<PaddlePage<TItem>>(envelopeResult.Error);
        }

        List<TItem>? items = envelopeResult.Value.Data;
        return items is null
            ? Result.Failure<PaddlePage<TItem>>(CreateProviderFailure("Paddle returned an empty list response."))
            : Result.Success(new PaddlePage<TItem>(items, NormalizeNext(envelopeResult.Value.Meta?.Pagination?.Next)));
    }

    private async Task<Result<PaddleEnvelope<TResponse>>> SendEnvelopeAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
        where TResponse : class {
        using HttpRequestMessage request = CreateRequest(method, path, body);
        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) {
            return Result.Failure<PaddleEnvelope<TResponse>>(CreateProviderFailure(
                $"Paddle returned HTTP status {((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)}."));
        }

        try {
            PaddleEnvelope<TResponse>? envelope = await BoundedHttpContentReader.ReadFromJsonAsync<PaddleEnvelope<TResponse>>(
                response.Content,
                JsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false);
            return envelope is null
                ? Result.Failure<PaddleEnvelope<TResponse>>(CreateProviderFailure("Paddle returned an empty response."))
                : Result.Success(envelope);
        } catch (Exception exception) when (exception is InvalidDataException or TimeoutException or JsonException) {
            return Result.Failure<PaddleEnvelope<TResponse>>(CreateProviderFailure(
                "Paddle returned an invalid or oversized response."));
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string path, object? body) {
        var request = new HttpRequestMessage(
            method,
            new Uri(new Uri(options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute), NormalizeRelativePath(path)));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Paddle-Version", "1");
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        return request;
    }

    private static string NormalizeRelativePath(string path) =>
        Uri.TryCreate(path, UriKind.Absolute, out Uri? absolute)
            ? absolute.PathAndQuery.TrimStart('/')
            : path.TrimStart('/');

    private static string? NormalizeNext(string? next) =>
        string.IsNullOrWhiteSpace(next) ? null : NormalizeRelativePath(next);

    private static Error CreateProviderFailure(string message) =>
        Errors.Billing.ProviderOperationFailed(BillingProviderNames.Paddle, message);

    private sealed record PaddleEnvelope<T>(
        [property: JsonPropertyName("data")] T? Data,
        [property: JsonPropertyName("meta")] ResponseMeta? Meta);

    private sealed record ResponseMeta(
        [property: JsonPropertyName("pagination")] PaginationMeta? Pagination);

    private sealed record PaginationMeta(
        [property: JsonPropertyName("next")] string? Next);
}
