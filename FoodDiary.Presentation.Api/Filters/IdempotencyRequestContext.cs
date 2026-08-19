using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Filters;

public static class IdempotencyRequestContext {
    private static readonly object RequestIdKey = new();
    private static readonly object RequestHashKey = new();

    public static string? GetRequestId(HttpContext context) =>
        context.Items.TryGetValue(RequestIdKey, out object? value) ? value as string : null;

    public static string? GetRequestHash(HttpContext context) =>
        context.Items.TryGetValue(RequestHashKey, out object? value) ? value as string : null;

    internal static void SetRequestId(HttpContext context, string requestId) =>
        context.Items[RequestIdKey] = requestId;

    internal static void SetRequest(HttpContext context, string requestId, string requestHash) {
        context.Items[RequestIdKey] = requestId;
        context.Items[RequestHashKey] = requestHash;
    }
}
