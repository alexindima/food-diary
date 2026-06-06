using FoodDiary.Application.Abstractions.Ai.Common;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Services;

internal sealed class AiPromptProvider(
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory) : IAiPromptProvider {
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private static readonly Dictionary<string, string> Fallbacks = new(StringComparer.OrdinalIgnoreCase) {
        ["vision"] = "Analyze the food photo and return only JSON with list of items. Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). Use grams (g) when possible. {{languageHint}}",
        ["text-parse"] = "Parse the following food description into structured items: \"{{userText}}\". Return only JSON with list of items. Each item must include nameEn, nameLocal, amount, unit, confidence (0-1). Use grams (g) when possible. Estimate typical portion sizes for items without explicit amounts. {{languageHint}}",
        ["nutrition"] = "You are a nutrition assistant. Using the provided items with amounts, estimate calories and nutrients per item and totals. Item names are in English. Return only JSON.",
    };

    public async Task<string> GetPromptAsync(string key, CancellationToken cancellationToken = default) {
        string cacheKey = $"ai-prompt:{key}";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null) {
            return cached;
        }

        string? promptText = null;
        using (IServiceScope scope = scopeFactory.CreateScope()) {
            FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            AiPromptTemplate? template = await context.Set<Domain.Entities.Ai.AiPromptTemplate>()
                .AsNoTracking()
                .Where(t => t.Key == key && t.IsActive)
                .OrderByDescending(t => t.Version)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            promptText = template?.PromptText;
        }

        string result = promptText ?? (Fallbacks.TryGetValue(key, out string? fallback) ? fallback : key);
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}
