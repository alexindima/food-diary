using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.Entities.Content;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.Entities.Recipes;
using FoodDiary.Domain.Entities.Shopping;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class EmailTemplateRepository(FoodDiaryDbContext context) : IEmailTemplateRepository
{
    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await context.EmailTemplates
            .AsNoTracking()
            .OrderBy(t => t.Key)
            .ThenBy(t => t.Locale)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailTemplate?> GetByKeyAsync(string key, string locale, CancellationToken cancellationToken = default)
    {
        return await context.EmailTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken);
    }

    public async Task<EmailTemplate> UpsertAsync(
        string key,
        string locale,
        string subject,
        string htmlBody,
        string textBody,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var existing = await context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Key == key && t.Locale == locale, cancellationToken);

        if (existing is null)
        {
            var template = EmailTemplate.Create(key, locale, subject, htmlBody, textBody, isActive);
            await context.EmailTemplates.AddAsync(template, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            return template;
        }

        existing.Update(subject, htmlBody, textBody, isActive);
        await context.SaveChangesAsync(cancellationToken);
        return existing;
    }
}

