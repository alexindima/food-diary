using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Filters;

public static class IdempotencyRequestContext {
    private static readonly object RequestIdKey = new();
    private static readonly object RequestHashKey = new();
    private static readonly object RetentionKey = new();

    public static FoodDiary.Application.Contracts.Common.Abstractions.Persistence.AtomicOperation? GetAtomicOperation(HttpContext context, Guid userId) =>
        GetRequestId(context) is { } key && GetRequestHash(context) is { } hash
            ? new(userId, key, hash, context.Items.TryGetValue(RetentionKey, out object? value) && value is TimeSpan retention
                ? retention : TimeSpan.FromDays(1))
            : null;

    internal static void SetRetention(HttpContext context, TimeSpan retention) => context.Items[RetentionKey] = retention;

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
