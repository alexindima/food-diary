using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Filters;

public static class IdempotencyRequestContext {
    private static readonly object RequestIdKey = new();

    public static string? GetRequestId(HttpContext context) =>
        context.Items.TryGetValue(RequestIdKey, out object? value) ? value as string : null;

    internal static void SetRequestId(HttpContext context, string requestId) =>
        context.Items[RequestIdKey] = requestId;
}
