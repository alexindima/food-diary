using FoodDiary.Modules.Ai.Application.Abstractions.Prompts;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Ai.Application.Abstractions.Common;
using FoodDiary.Modules.Ai.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Ai.Infrastructure.Persistence;

internal sealed class AiPromptProvider(
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory) : IAiPromptProvider {
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<string> GetPromptAsync(string key, string? language, CancellationToken cancellationToken = default) {
        string locale = LanguageCode.FromPreferred(language).Value;
        string cacheKey = $"ai-prompt:{key}:{locale}";
        if (cache.TryGetValue(cacheKey, out string? cached) && cached is not null) {
            return cached;
        }

        string? promptText;
        using (IServiceScope scope = scopeFactory.CreateScope()) {
            AiDbContext context = scope.ServiceProvider.GetRequiredService<AiDbContext>();
            AiPromptTemplate? template = await context.Set<AiPromptTemplate>()
                .AsNoTracking()
                .Where(t => t.Key == key && t.IsActive && (t.Locale == locale || t.Locale == "en"))
                .OrderByDescending(t => t.Locale == locale)
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            promptText = template?.PromptText;
        }

        string result = promptText ?? AiPromptCatalog.GetDefault(key) ?? key;
        cache.Set(cacheKey, result, CacheDuration);
        return result;
    }
}
