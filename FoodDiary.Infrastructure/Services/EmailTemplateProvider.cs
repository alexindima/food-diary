using System;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Application.Common.Models;
using FoodDiary.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Infrastructure.Services;

public sealed class EmailTemplateProvider(
    IServiceScopeFactory scopeFactory,
    IMemoryCache cache) : IEmailTemplateProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    public async Task<EmailTemplateContent?> GetActiveTemplateAsync(string key, string locale, CancellationToken cancellationToken = default)
    {
        var normalizedKey = NormalizeKey(key);
        var normalizedLocale = NormalizeLocale(locale);
        var cacheKey = $"email-template:{normalizedKey}:{normalizedLocale}";
        if (cache.TryGetValue(cacheKey, out EmailTemplateContent? cached))
        {
            return cached;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();

        var template = await db.EmailTemplates
            .AsNoTracking()
            .Where(t => t.Key == normalizedKey && t.Locale == normalizedLocale && t.IsActive)
            .Select(t => new EmailTemplateContent(t.Subject, t.HtmlBody, t.TextBody))
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null && normalizedLocale != "en")
        {
            template = await db.EmailTemplates
                .AsNoTracking()
                .Where(t => t.Key == normalizedKey && t.Locale == "en" && t.IsActive)
                .Select(t => new EmailTemplateContent(t.Subject, t.HtmlBody, t.TextBody))
                .FirstOrDefaultAsync(cancellationToken);
        }

        cache.Set(cacheKey, template, CacheDuration);
        return template;
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return "en";
        }

        var lower = locale.Trim().ToLowerInvariant();
        return lower.StartsWith("ru") ? "ru" : "en";
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim().ToLowerInvariant();
    }
}
