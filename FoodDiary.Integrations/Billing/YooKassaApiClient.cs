using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Domain.Entities.Billing;
using FoodDiary.Integrations.Http;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;

namespace FoodDiary.Integrations.Billing;

internal sealed class YooKassaApiClient(HttpClient httpClient, YooKassaOptions options) {
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        MaxDepth = BoundedHttpContentReader.DefaultJsonMaxDepth,
    };

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public async Task<Result<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        string? idempotenceKey,
        CancellationToken cancellationToken)
        where TResponse : class {
        using var request = new HttpRequestMessage(
            method,
            new Uri(new Uri(options.ApiBaseUrl.TrimEnd('/') + "/", UriKind.Absolute), path));
        byte[] authBytes = Encoding.UTF8.GetBytes($"{options.ShopId}:{options.SecretKey}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (method != HttpMethod.Get) {
            request.Headers.Add("Idempotence-Key", ResolveIdempotenceKey(idempotenceKey));
        }

        if (body is not null) {
            request.Content = JsonContent.Create(body, options: JsonOptions);
        }

        try {
            using HttpResponseMessage response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) {
                return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                    BillingProviderNames.YooKassa,
                    $"YooKassa returned HTTP status {((int)response.StatusCode).ToString(CultureInfo.InvariantCulture)}."));
            }

            TResponse? result = await BoundedHttpContentReader.ReadFromJsonAsync<TResponse>(
                response.Content,
                JsonOptions,
                BoundedHttpContentReader.DefaultMaxResponseBodyBytes,
                BoundedHttpContentReader.DefaultReadTimeout,
                cancellationToken).ConfigureAwait(false);
            return result is null
                ? Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                    BillingProviderNames.YooKassa,
                    "YooKassa returned an empty response."))
                : Result.Success(result);
        } catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                BillingProviderNames.YooKassa,
                "YooKassa request timed out."));
        } catch (HttpRequestException) when (!cancellationToken.IsCancellationRequested) {
            return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                BillingProviderNames.YooKassa,
                "YooKassa request could not be completed."));
        } catch (Exception exception) when (exception is InvalidDataException or TimeoutException or JsonException) {
            return Result.Failure<TResponse>(Errors.Billing.ProviderOperationFailed(
                BillingProviderNames.YooKassa,
                "YooKassa returned an invalid or oversized response."));
        }
    }

    private static string ResolveIdempotenceKey(string? idempotenceKey) =>
        string.IsNullOrWhiteSpace(idempotenceKey)
            ? Guid.NewGuid().ToString("N")
            : idempotenceKey.Trim();
}
