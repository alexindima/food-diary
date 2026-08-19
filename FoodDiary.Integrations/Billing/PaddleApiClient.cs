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
        using var request = new HttpRequestMessage(
            method,
            new Uri(new Uri(options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("Paddle-Version", "1");
        if (body is not null) {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        using HttpResponseMessage response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);
        try {
            if (!response.IsSuccessStatusCode) {
                return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                    BillingProviderNames.Paddle,
                    $"Paddle returned HTTP status {((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)}."));
            }

            PaddleEnvelope<TResponse>? envelope = await BoundedHttpContentReader.ReadFromJsonAsync<PaddleEnvelope<TResponse>>(
                response.Content,
                JsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false);
            return envelope?.Data is null
                ? Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                    BillingProviderNames.Paddle,
                    "Paddle returned an empty response."))
                : Result.Success(envelope.Data);
        } catch (Exception exception) when (exception is InvalidDataException or TimeoutException or JsonException) {
            return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                BillingProviderNames.Paddle,
                "Paddle returned an invalid or oversized response."));
        }
    }

    private sealed record PaddleEnvelope<T>(
        [property: JsonPropertyName("data")] T? Data);
}
