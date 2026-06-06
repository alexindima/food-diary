using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Services;

public sealed class EmailTemplateProvider(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache) : IEmailTemplateProvider {
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<EmailTemplateContent?> GetActiveTemplateAsync(string key, string locale, CancellationToken cancellationToken = default) {
        string normalizedKey = NormalizeKey(key);
        string normalizedLocale = NormalizeLocale(locale);
        string cacheKey = $"email-template:{normalizedKey}:{normalizedLocale}";
        if (cache.TryGetValue(cacheKey, out EmailTemplateContent? cached)) {
            return cached;
        }

        AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false)) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();

            EmailTemplateContent? template = await db.EmailTemplates
                .AsNoTracking()
                .Where(t => t.Key == normalizedKey && t.Locale == normalizedLocale && t.IsActive)
                .Select(t => new EmailTemplateContent(t.Subject, t.HtmlBody, t.TextBody))
                .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

            if (template is null && !string.Equals(normalizedLocale, "en", StringComparison.Ordinal)) {
                template = await db.EmailTemplates
                    .AsNoTracking()
                    .Where(t => t.Key == normalizedKey && t.Locale == "en" && t.IsActive)
                    .Select(t => new EmailTemplateContent(t.Subject, t.HtmlBody, t.TextBody))
                    .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
            }

            cache.Set(cacheKey, template, CacheDuration);
            return template;
        }
    }

    private static string NormalizeLocale(string locale) {
        if (string.IsNullOrWhiteSpace(locale)) {
            return "en";
        }

        string lower = locale.Trim().ToLowerInvariant();
        return lower.StartsWith("ru", StringComparison.Ordinal) ? "ru" : "en";
    }

    private static string NormalizeKey(string key) {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }
}
